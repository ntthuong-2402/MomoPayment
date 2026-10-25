using Microsoft.EntityFrameworkCore;
using MomoPayment.Caching;
using MomoPayment.Common.DTOs;
using MomoPayment.Common.Enums;
using MomoPayment.Common.Exceptions;
using MomoPayment.EventBus;
using MomoPayment.EventBus.Events;
using Wallet.API.Data;
using Wallet.API.DTOs;
using Wallet.API.Entities;

namespace Wallet.API.Services;

public interface IWalletService
{
    Task<WalletResponse> GetOrCreateWalletAsync(Guid userId, string phoneNumber, CancellationToken cancellationToken = default);
    Task<WalletResponse> GetBalanceAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<WalletResponse> DepositAsync(Guid userId, DepositRequest request, string? transactionCode = null, CancellationToken cancellationToken = default);
    Task<WalletResponse> WithdrawAsync(Guid userId, WithdrawRequest request, string? transactionCode = null, CancellationToken cancellationToken = default);
    Task<PagedResult<LedgerResponse>> GetLedgerAsync(Guid userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default);
    Task<(bool Success, decimal SenderNewBalance, decimal ReceiverNewBalance, string? ErrorCode, string? ErrorMessage)> TransferFundsInternalAsync(
        Guid senderUserId,
        Guid receiverUserId,
        decimal amount,
        string transactionCode,
        string description,
        CancellationToken cancellationToken = default);
}

public class WalletService : IWalletService
{
    private readonly WalletDbContext _dbContext;
    private readonly IDistributedLockService _lockService;
    private readonly ICacheService _cacheService;
    private readonly IEventBus _eventBus;
    private readonly ILogger<WalletService> _logger;

    public WalletService(
        WalletDbContext dbContext,
        IDistributedLockService lockService,
        ICacheService cacheService,
        IEventBus eventBus,
        ILogger<WalletService> logger)
    {
        _dbContext = dbContext;
        _lockService = lockService;
        _cacheService = cacheService;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<WalletResponse> GetOrCreateWalletAsync(Guid userId, string phoneNumber, CancellationToken cancellationToken = default)
    {
        var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);
        if (wallet == null)
        {
            wallet = new WalletAccount
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                PhoneNumber = phoneNumber,
                Balance = 0m,
                Currency = "VND",
                Status = WalletStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            await _dbContext.Wallets.AddAsync(wallet, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return MapToResponse(wallet);
    }

    public async Task<WalletResponse> GetBalanceAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"wallet_balance:{userId}";
        var cached = await _cacheService.GetAsync<WalletResponse>(cacheKey, cancellationToken);
        if (cached != null) return cached;

        var wallet = await _dbContext.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);
        if (wallet == null)
        {
            throw new NotFoundException("Ví người dùng không tồn tại", "WALLET_NOT_FOUND");
        }

        var response = MapToResponse(wallet);
        await _cacheService.SetAsync(cacheKey, response, TimeSpan.FromSeconds(30), cancellationToken);
        return response;
    }

    public async Task<WalletResponse> DepositAsync(Guid userId, DepositRequest request, string? transactionCode = null, CancellationToken cancellationToken = default)
    {
        var lockKey = $"wallet_lock:{userId}";
        var lockValue = Guid.NewGuid().ToString("N");

        bool acquired = await _lockService.AcquireLockAsync(
            lockKey,
            lockValue,
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(50));

        if (!acquired)
        {
            throw new ConcurrencyConflictException("Hệ thống đang xử lý giao dịch khác cho ví của bạn. Vui lòng thử lại");
        }

        try
        {
            var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);
            if (wallet == null)
            {
                throw new NotFoundException("Ví người dùng không tồn tại", "WALLET_NOT_FOUND");
            }

            if (wallet.Status != WalletStatus.Active)
            {
                throw new BadRequestException("Ví đang bị khóa hoặc tạm ngưng hoạt động", "WALLET_INACTIVE");
            }

            var previousBalance = wallet.Balance;
            wallet.Balance += request.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            var txnCode = !string.IsNullOrWhiteSpace(transactionCode)
                ? transactionCode
                : $"DEP-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

            var ledger = new WalletLedger
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                PreviousBalance = previousBalance,
                NewBalance = wallet.Balance,
                Amount = request.Amount,
                Type = "Credit",
                TransactionCode = txnCode,
                Description = request.Description ?? $"Nạp tiền từ nguồn {request.PaymentSource}",
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.Ledgers.AddAsync(ledger, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Invalidate cache
            await _cacheService.RemoveAsync($"wallet_balance:{userId}", cancellationToken);

            // Event to RabbitMQ
            await _eventBus.PublishAsync(new WalletBalanceChangedEvent
            {
                WalletId = wallet.Id,
                UserId = wallet.UserId,
                PhoneNumber = wallet.PhoneNumber,
                OldBalance = previousBalance,
                NewBalance = wallet.Balance,
                Amount = request.Amount,
                ChangeType = "Credit",
                TransactionCode = txnCode,
                Timestamp = DateTime.UtcNow
            }, cancellationToken);

            return MapToResponse(wallet);
        }
        finally
        {
            await _lockService.ReleaseLockAsync(lockKey, lockValue);
        }
    }

    public async Task<WalletResponse> WithdrawAsync(Guid userId, WithdrawRequest request, string? transactionCode = null, CancellationToken cancellationToken = default)
    {
        var lockKey = $"wallet_lock:{userId}";
        var lockValue = Guid.NewGuid().ToString("N");

        bool acquired = await _lockService.AcquireLockAsync(
            lockKey,
            lockValue,
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMilliseconds(50));

        if (!acquired)
        {
            throw new ConcurrencyConflictException("Hệ thống đang xử lý giao dịch khác cho ví của bạn. Vui lòng thử lại");
        }

        try
        {
            var wallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);
            if (wallet == null)
            {
                throw new NotFoundException("Ví người dùng không tồn tại", "WALLET_NOT_FOUND");
            }

            if (wallet.Status != WalletStatus.Active)
            {
                throw new BadRequestException("Ví đang bị khóa hoặc tạm ngưng hoạt động", "WALLET_INACTIVE");
            }

            if (wallet.Balance < request.Amount)
            {
                throw new InsufficientBalanceException($"Số dư hiện tại ({wallet.Balance:N0} VND) không đủ để rút {request.Amount:N0} VND");
            }

            var previousBalance = wallet.Balance;
            wallet.Balance -= request.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;

            var txnCode = !string.IsNullOrWhiteSpace(transactionCode)
                ? transactionCode
                : $"WIT-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

            var ledger = new WalletLedger
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                PreviousBalance = previousBalance,
                NewBalance = wallet.Balance,
                Amount = request.Amount,
                Type = "Debit",
                TransactionCode = txnCode,
                Description = request.Description ?? $"Rút tiền về ngân hàng {request.BankCode} - STK {request.BankAccountNumber}",
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.Ledgers.AddAsync(ledger, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Invalidate cache
            await _cacheService.RemoveAsync($"wallet_balance:{userId}", cancellationToken);

            // Event to RabbitMQ
            await _eventBus.PublishAsync(new WalletBalanceChangedEvent
            {
                WalletId = wallet.Id,
                UserId = wallet.UserId,
                PhoneNumber = wallet.PhoneNumber,
                OldBalance = previousBalance,
                NewBalance = wallet.Balance,
                Amount = request.Amount,
                ChangeType = "Debit",
                TransactionCode = txnCode,
                Timestamp = DateTime.UtcNow
            }, cancellationToken);

            return MapToResponse(wallet);
        }
        finally
        {
            await _lockService.ReleaseLockAsync(lockKey, lockValue);
        }
    }

    public async Task<PagedResult<LedgerResponse>> GetLedgerAsync(Guid userId, int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var wallet = await _dbContext.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);
        if (wallet == null)
        {
            throw new NotFoundException("Ví người dùng không tồn tại", "WALLET_NOT_FOUND");
        }

        var query = _dbContext.Ledgers
            .AsNoTracking()
            .Where(l => l.WalletId == wallet.Id)
            .OrderByDescending(l => l.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new LedgerResponse
            {
                Id = l.Id,
                PreviousBalance = l.PreviousBalance,
                NewBalance = l.NewBalance,
                Amount = l.Amount,
                Type = l.Type,
                TransactionCode = l.TransactionCode,
                Description = l.Description,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<LedgerResponse>(items, totalCount, pageIndex, pageSize);
    }

    public async Task<(bool Success, decimal SenderNewBalance, decimal ReceiverNewBalance, string? ErrorCode, string? ErrorMessage)> TransferFundsInternalAsync(
        Guid senderUserId,
        Guid receiverUserId,
        decimal amount,
        string transactionCode,
        string description,
        CancellationToken cancellationToken = default)
    {
        if (senderUserId == receiverUserId)
        {
            return (false, 0, 0, "INVALID_TRANSFER", "Không thể tự chuyển tiền cho chính mình");
        }

        // Distributed locking on both wallets in deterministic alphabetical order to avoid deadlocks
        var lockKeys = new[] { $"wallet_lock:{senderUserId}", $"wallet_lock:{receiverUserId}" };

        await using var lockHandle = await _lockService.AcquireLocksInOrderAsync(
            lockKeys,
            TimeSpan.FromSeconds(15),
            TimeSpan.FromSeconds(8));

        if (lockHandle == null)
        {
            return (false, 0, 0, "LOCK_TIMEOUT", "Không thể lấy khóa ví để xử lý giao dịch. Vui lòng thử lại");
        }

        // Guard against duplicate execution if retried with the same transaction code
        var existingDebit = await _dbContext.Ledgers.AsNoTracking().FirstOrDefaultAsync(l => l.TransactionCode == transactionCode && l.Type == "Debit", cancellationToken);
        if (existingDebit != null)
        {
            _logger.LogInformation("Transaction {TxnCode} has already been processed in WalletLedger. Returning current balances.", transactionCode);
            var sWallet = await _dbContext.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.UserId == senderUserId, cancellationToken);
            var rWallet = await _dbContext.Wallets.AsNoTracking().FirstOrDefaultAsync(w => w.UserId == receiverUserId, cancellationToken);
            return (true, sWallet?.Balance ?? 0, rWallet?.Balance ?? 0, null, null);
        }

        // Use EF Core database transaction
        using var dbTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var senderWallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == senderUserId, cancellationToken);
            if (senderWallet == null)
            {
                return (false, 0, 0, "SENDER_WALLET_NOT_FOUND", "Không tìm thấy ví người gửi");
            }

            var receiverWallet = await _dbContext.Wallets.FirstOrDefaultAsync(w => w.UserId == receiverUserId, cancellationToken);
            if (receiverWallet == null)
            {
                return (false, 0, 0, "RECEIVER_WALLET_NOT_FOUND", "Không tìm thấy ví người nhận");
            }

            if (senderWallet.Status != WalletStatus.Active)
            {
                return (false, 0, 0, "SENDER_WALLET_INACTIVE", "Ví người gửi đang bị tạm khóa");
            }

            if (receiverWallet.Status != WalletStatus.Active)
            {
                return (false, 0, 0, "RECEIVER_WALLET_INACTIVE", "Ví người nhận đang bị tạm khóa");
            }

            if (senderWallet.Balance < amount)
            {
                return (false, senderWallet.Balance, receiverWallet.Balance, "INSUFFICIENT_BALANCE", "Số dư ví người gửi không đủ để chuyển tiền");
            }

            // Deduct sender
            var senderPrev = senderWallet.Balance;
            senderWallet.Balance -= amount;
            senderWallet.UpdatedAt = DateTime.UtcNow;

            // Credit receiver
            var receiverPrev = receiverWallet.Balance;
            receiverWallet.Balance += amount;
            receiverWallet.UpdatedAt = DateTime.UtcNow;

            // Create ledger entries
            var senderLedger = new WalletLedger
            {
                Id = Guid.NewGuid(),
                WalletId = senderWallet.Id,
                PreviousBalance = senderPrev,
                NewBalance = senderWallet.Balance,
                Amount = amount,
                Type = "Debit",
                TransactionCode = transactionCode,
                Description = $"Chuyển tiền MoMo: {description}",
                CreatedAt = DateTime.UtcNow
            };

            var receiverLedger = new WalletLedger
            {
                Id = Guid.NewGuid(),
                WalletId = receiverWallet.Id,
                PreviousBalance = receiverPrev,
                NewBalance = receiverWallet.Balance,
                Amount = amount,
                Type = "Credit",
                TransactionCode = transactionCode,
                Description = $"Nhận tiền MoMo: {description}",
                CreatedAt = DateTime.UtcNow
            };

            await _dbContext.Ledgers.AddRangeAsync(new[] { senderLedger, receiverLedger }, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await dbTransaction.CommitAsync(cancellationToken);

            // Invalidate redis cache for both users
            await _cacheService.RemoveAsync($"wallet_balance:{senderUserId}", cancellationToken);
            await _cacheService.RemoveAsync($"wallet_balance:{receiverUserId}", cancellationToken);

            _logger.LogInformation("P2P Transfer completed: {TxnCode}. Sender: {Sender} (-{Amount}), Receiver: {Receiver} (+{Amount})",
                transactionCode, senderUserId, amount, receiverUserId, amount);

            return (true, senderWallet.Balance, receiverWallet.Balance, null, null);
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed to process internal transfer {TxnCode}", transactionCode);
            return (false, 0, 0, "INTERNAL_ERROR", "Lỗi xử lý giao dịch chuyển tiền");
        }
    }

    private static WalletResponse MapToResponse(WalletAccount w) => new()
    {
        Id = w.Id,
        UserId = w.UserId,
        PhoneNumber = w.PhoneNumber,
        Balance = w.Balance,
        Currency = w.Currency,
        Status = w.Status
    };
}
