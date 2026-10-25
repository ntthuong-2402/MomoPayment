using Microsoft.EntityFrameworkCore;
using MomoPayment.Caching;
using MomoPayment.Common.DTOs;
using MomoPayment.Common.Enums;
using MomoPayment.Common.Exceptions;
using MomoPayment.EventBus;
using MomoPayment.EventBus.Events;
using MomoPayment.Protos;
using Transaction.API.Data;
using Transaction.API.DTOs;
using Transaction.API.Entities;

namespace Transaction.API.Services;

public interface ITransactionService
{
    Task<TransactionResponse> TransferMoneyAsync(Guid senderId, string senderPhone, TransferRequest request, CancellationToken cancellationToken = default);
    Task<TransactionResponse> MakePaymentAsync(Guid senderId, string senderPhone, PaymentRequest request, CancellationToken cancellationToken = default);
    Task<TransactionResponse> GetTransactionByCodeAsync(string code, Guid userId, CancellationToken cancellationToken = default);
    Task<PagedResult<TransactionResponse>> GetHistoryAsync(Guid userId, TransactionFilterRequest filter, CancellationToken cancellationToken = default);
}

public class TransactionService : ITransactionService
{
    private readonly TransactionDbContext _dbContext;
    private readonly WalletGrpc.WalletGrpcClient _walletClient;
    private readonly IEventBus _eventBus;
    private readonly IIdempotencyService _idempotencyService;
    private readonly ILogger<TransactionService> _logger;

    public TransactionService(
        TransactionDbContext dbContext,
        WalletGrpc.WalletGrpcClient walletClient,
        IEventBus eventBus,
        IIdempotencyService idempotencyService,
        ILogger<TransactionService> logger)
    {
        _dbContext = dbContext;
        _walletClient = walletClient;
        _eventBus = eventBus;
        _idempotencyService = idempotencyService;
        _logger = logger;
    }

    public async Task<TransactionResponse> TransferMoneyAsync(Guid senderId, string senderPhone, TransferRequest request, CancellationToken cancellationToken = default)
    {
        var receiverPhone = request.ReceiverPhone.Trim();
        if (senderPhone == receiverPhone)
        {
            throw new BadRequestException("Không thể tự chuyển tiền cho số điện thoại của chính mình", "SELF_TRANSFER_NOT_ALLOWED");
        }

        // Idempotency check via Redis
        var idempotencyKey = request.IdempotencyKey;
        bool keyAcquired = false;
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var cached = await _idempotencyService.GetResultAsync<TransactionResponse>(idempotencyKey);
            if (cached != null)
            {
                _logger.LogInformation("Returning cached transaction response for idempotency key {Key}", idempotencyKey);
                return cached;
            }

            bool acquired = await _idempotencyService.TryAcquireKeyAsync(idempotencyKey, TimeSpan.FromMinutes(5));
            if (!acquired)
            {
                throw new DuplicateTransactionException();
            }
            keyAcquired = true;
        }

        try
        {
            // Step 1: Verify recipient wallet via gRPC
            var verifyResp = await _walletClient.VerifyWalletAsync(new VerifyWalletRequest
            {
                PhoneNumber = receiverPhone
            }, cancellationToken: cancellationToken);

            if (!verifyResp.Exists)
            {
                throw new NotFoundException($"Số điện thoại {receiverPhone} chưa kích hoạt ví MoMo", "RECIPIENT_WALLET_NOT_FOUND");
            }

            if (!verifyResp.IsActive)
            {
                throw new BadRequestException("Ví người nhận hiện đang bị khóa", "RECIPIENT_WALLET_LOCKED");
            }

            var receiverUserId = Guid.Parse(verifyResp.UserId);

            // Step 2: Create Pending Transaction record
            var txnCode = $"MOMO-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
            var transaction = new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                TransactionCode = txnCode,
                SenderUserId = senderId,
                SenderPhone = senderPhone,
                ReceiverUserId = receiverUserId,
                ReceiverPhone = receiverPhone,
                Amount = request.Amount,
                Fee = 0m, // Miễn phí chuyển tiền MoMo
                NetAmount = request.Amount,
                Type = TransactionType.Transfer,
                Status = TransactionStatus.Pending,
                Description = request.Description,
                IdempotencyKey = idempotencyKey,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.Transactions.AddAsync(transaction, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Step 3: Execute transfer via internal gRPC to Wallet Service
            var transferResp = await _walletClient.TransferFundsAsync(new TransferFundsRequest
            {
                SenderUserId = senderId.ToString(),
                ReceiverUserId = receiverUserId.ToString(),
                Amount = (double)request.Amount,
                TransactionCode = txnCode,
                Description = request.Description ?? "Chuyển tiền MoMo"
            }, cancellationToken: cancellationToken);

            if (!transferResp.Success)
            {
                transaction.Status = TransactionStatus.Failed;
                transaction.FailureReason = transferResp.ErrorMessage;
                transaction.CompletedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);

                if (transferResp.ErrorCode == "INSUFFICIENT_BALANCE")
                {
                    throw new InsufficientBalanceException(transferResp.ErrorMessage);
                }

                throw new BadRequestException(transferResp.ErrorMessage, transferResp.ErrorCode);
            }

            // Step 4: Transaction succeeded
            transaction.Status = TransactionStatus.Success;
            transaction.CompletedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            var response = MapToResponse(transaction);

            // Cache result for idempotency
            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                await _idempotencyService.SaveResultAsync(idempotencyKey, response, TimeSpan.FromHours(24));
            }

            // Step 5: Asynchronous event publishing to RabbitMQ
            await _eventBus.PublishAsync(new TransactionCompletedEvent
            {
                TransactionId = transaction.Id,
                TransactionCode = transaction.TransactionCode,
                SenderUserId = transaction.SenderUserId,
                SenderPhone = transaction.SenderPhone,
                ReceiverUserId = transaction.ReceiverUserId,
                ReceiverPhone = transaction.ReceiverPhone,
                Amount = transaction.Amount,
                Fee = transaction.Fee,
                Type = transaction.Type.ToString(),
                Status = transaction.Status.ToString(),
                Description = transaction.Description,
                CompletedAt = transaction.CompletedAt!.Value
            }, cancellationToken);

            _logger.LogInformation("Transaction {TxnCode} completed successfully and event published", txnCode);
            return response;
        }
        catch
        {
            if (keyAcquired && !string.IsNullOrWhiteSpace(idempotencyKey))
            {
                await _idempotencyService.ReleaseKeyAsync(idempotencyKey);
            }
            throw;
        }
    }

    public async Task<TransactionResponse> MakePaymentAsync(Guid senderId, string senderPhone, PaymentRequest request, CancellationToken cancellationToken = default)
    {
        var idempotencyKey = request.IdempotencyKey;
        bool keyAcquired = false;
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var cached = await _idempotencyService.GetResultAsync<TransactionResponse>(idempotencyKey);
            if (cached != null) return cached;

            bool acquired = await _idempotencyService.TryAcquireKeyAsync(idempotencyKey, TimeSpan.FromMinutes(5));
            if (!acquired) throw new DuplicateTransactionException();
            keyAcquired = true;
        }

        try
        {
            var txnCode = $"PAY-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
            var transaction = new PaymentTransaction
            {
                Id = Guid.NewGuid(),
                TransactionCode = txnCode,
                SenderUserId = senderId,
                SenderPhone = senderPhone,
                Amount = request.Amount,
                Fee = 0m,
                NetAmount = request.Amount,
                Type = TransactionType.Payment,
                Status = TransactionStatus.Pending,
                Description = $"Thanh toán đơn hàng {request.OrderId} tại {request.MerchantId}: {request.Description}",
                IdempotencyKey = idempotencyKey,
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.Transactions.AddAsync(transaction, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Withdraw / deduct payment amount from wallet
            var withdrawResp = await _walletClient.WithdrawFundsAsync(new WithdrawFundsRequest
            {
                UserId = senderId.ToString(),
                Amount = (double)request.Amount,
                TransactionCode = txnCode,
                BankCode = request.MerchantId ?? "MERCHANT",
                BankAccountNumber = request.OrderId ?? "ORDER"
            }, cancellationToken: cancellationToken);

            if (!withdrawResp.Success)
            {
                transaction.Status = TransactionStatus.Failed;
                transaction.FailureReason = withdrawResp.ErrorMessage;
                transaction.CompletedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync(cancellationToken);

                throw new BadRequestException(withdrawResp.ErrorMessage, withdrawResp.ErrorCode);
            }

            transaction.Status = TransactionStatus.Success;
            transaction.CompletedAt = DateTime.UtcNow;
            await _dbContext.SaveChangesAsync(cancellationToken);

            var response = MapToResponse(transaction);

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                await _idempotencyService.SaveResultAsync(idempotencyKey, response, TimeSpan.FromHours(24));
            }

            // Publish event to RabbitMQ
            await _eventBus.PublishAsync(new TransactionCompletedEvent
            {
                TransactionId = transaction.Id,
                TransactionCode = transaction.TransactionCode,
                SenderUserId = transaction.SenderUserId,
                SenderPhone = transaction.SenderPhone,
                Amount = transaction.Amount,
                Fee = transaction.Fee,
                Type = transaction.Type.ToString(),
                Status = transaction.Status.ToString(),
                Description = transaction.Description,
                CompletedAt = transaction.CompletedAt!.Value
            }, cancellationToken);

            return response;
        }
        catch
        {
            if (keyAcquired && !string.IsNullOrWhiteSpace(idempotencyKey))
            {
                await _idempotencyService.ReleaseKeyAsync(idempotencyKey);
            }
            throw;
        }
    }

    public async Task<TransactionResponse> GetTransactionByCodeAsync(string code, Guid userId, CancellationToken cancellationToken = default)
    {
        var txn = await _dbContext.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TransactionCode == code && (t.SenderUserId == userId || t.ReceiverUserId == userId), cancellationToken);

        if (txn == null)
        {
            throw new NotFoundException("Không tìm thấy thông tin giao dịch", "TRANSACTION_NOT_FOUND");
        }

        return MapToResponse(txn);
    }

    public async Task<PagedResult<TransactionResponse>> GetHistoryAsync(Guid userId, TransactionFilterRequest filter, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Transactions
            .AsNoTracking()
            .Where(t => t.SenderUserId == userId || t.ReceiverUserId == userId);

        if (filter.Type.HasValue)
        {
            query = query.Where(t => t.Type == filter.Type.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(t => t.Status == filter.Status.Value);
        }

        if (filter.FromDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= filter.FromDate.Value);
        }

        if (filter.ToDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= filter.ToDate.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => MapToResponse(t))
            .ToListAsync(cancellationToken);

        return new PagedResult<TransactionResponse>(items, totalCount, page, pageSize);
    }

    private static TransactionResponse MapToResponse(PaymentTransaction t) => new()
    {
        Id = t.Id,
        TransactionCode = t.TransactionCode,
        SenderUserId = t.SenderUserId,
        SenderPhone = t.SenderPhone,
        ReceiverUserId = t.ReceiverUserId,
        ReceiverPhone = t.ReceiverPhone,
        Amount = t.Amount,
        Fee = t.Fee,
        NetAmount = t.NetAmount,
        Type = t.Type,
        Status = t.Status,
        FailureReason = t.FailureReason,
        Description = t.Description,
        CreatedAt = t.CreatedAt,
        CompletedAt = t.CompletedAt
    };
}
