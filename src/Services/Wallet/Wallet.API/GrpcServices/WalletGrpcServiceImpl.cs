using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using MomoPayment.Protos;
using Wallet.API.Data;
using Wallet.API.DTOs;
using Wallet.API.Services;

namespace Wallet.API.GrpcServices;

public class WalletGrpcServiceImpl : WalletGrpc.WalletGrpcBase
{
    private readonly IWalletService _walletService;
    private readonly WalletDbContext _dbContext;
    private readonly ILogger<WalletGrpcServiceImpl> _logger;

    public WalletGrpcServiceImpl(
        IWalletService walletService,
        WalletDbContext dbContext,
        ILogger<WalletGrpcServiceImpl> logger)
    {
        _walletService = walletService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public override async Task<GetBalanceResponse> GetBalance(GetBalanceRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.UserId, out var userId))
        {
            return new GetBalanceResponse { Success = false, ErrorMessage = "Invalid user_id" };
        }

        try
        {
            var wallet = await _walletService.GetBalanceAsync(userId, context.CancellationToken);
            return new GetBalanceResponse
            {
                Success = true,
                UserId = wallet.UserId.ToString(),
                WalletId = wallet.Id.ToString(),
                Balance = (double)wallet.Balance,
                Currency = wallet.Currency
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "gRPC GetBalance failed for user {UserId}", request.UserId);
            return new GetBalanceResponse { Success = false, ErrorMessage = ex.Message };
        }
    }

    public override async Task<TransferFundsResponse> TransferFunds(TransferFundsRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.SenderUserId, out var senderId) ||
            !Guid.TryParse(request.ReceiverUserId, out var receiverId))
        {
            return new TransferFundsResponse
            {
                Success = false,
                ErrorCode = "INVALID_ARGUMENTS",
                ErrorMessage = "Mã người dùng không hợp lệ"
            };
        }

        var result = await _walletService.TransferFundsInternalAsync(
            senderId,
            receiverId,
            (decimal)request.Amount,
            request.TransactionCode,
            request.Description,
            context.CancellationToken);

        return new TransferFundsResponse
        {
            Success = result.Success,
            SenderNewBalance = (double)result.SenderNewBalance,
            ReceiverNewBalance = (double)result.ReceiverNewBalance,
            ErrorCode = result.ErrorCode ?? string.Empty,
            ErrorMessage = result.ErrorMessage ?? string.Empty
        };
    }

    public override async Task<DepositFundsResponse> DepositFunds(DepositFundsRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.UserId, out var userId))
        {
            return new DepositFundsResponse { Success = false, ErrorCode = "INVALID_USER_ID", ErrorMessage = "Invalid user id" };
        }

        try
        {
            var wallet = await _walletService.DepositAsync(userId, new DepositRequest
            {
                Amount = (decimal)request.Amount,
                PaymentSource = request.PaymentSource,
                Description = $"Nạp tiền qua giao dịch {request.TransactionCode}"
            }, request.TransactionCode, context.CancellationToken);

            return new DepositFundsResponse
            {
                Success = true,
                NewBalance = (double)wallet.Balance
            };
        }
        catch (Exception ex)
        {
            return new DepositFundsResponse
            {
                Success = false,
                ErrorCode = "DEPOSIT_FAILED",
                ErrorMessage = ex.Message
            };
        }
    }

    public override async Task<WithdrawFundsResponse> WithdrawFunds(WithdrawFundsRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.UserId, out var userId))
        {
            return new WithdrawFundsResponse { Success = false, ErrorCode = "INVALID_USER_ID", ErrorMessage = "Invalid user id" };
        }

        try
        {
            var wallet = await _walletService.WithdrawAsync(userId, new WithdrawRequest
            {
                Amount = (decimal)request.Amount,
                BankCode = request.BankCode,
                BankAccountNumber = request.BankAccountNumber,
                Description = $"Rút tiền qua giao dịch {request.TransactionCode}"
            }, request.TransactionCode, context.CancellationToken);

            return new WithdrawFundsResponse
            {
                Success = true,
                NewBalance = (double)wallet.Balance
            };
        }
        catch (Exception ex)
        {
            return new WithdrawFundsResponse
            {
                Success = false,
                ErrorCode = "WITHDRAW_FAILED",
                ErrorMessage = ex.Message
            };
        }
    }

    public override async Task<VerifyWalletResponse> VerifyWallet(VerifyWalletRequest request, ServerCallContext context)
    {
        var query = _dbContext.Wallets.AsNoTracking();

        if (Guid.TryParse(request.UserId, out var userId))
        {
            query = query.Where(w => w.UserId == userId);
        }
        else if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            query = query.Where(w => w.PhoneNumber == request.PhoneNumber.Trim());
        }
        else
        {
            return new VerifyWalletResponse { Exists = false, ErrorMessage = "Phải cung cấp UserId hoặc PhoneNumber" };
        }

        var wallet = await query.FirstOrDefaultAsync(context.CancellationToken);
        if (wallet == null)
        {
            return new VerifyWalletResponse { Exists = false, ErrorMessage = "Không tìm thấy ví" };
        }

        return new VerifyWalletResponse
        {
            Exists = true,
            UserId = wallet.UserId.ToString(),
            WalletId = wallet.Id.ToString(),
            PhoneNumber = wallet.PhoneNumber,
            IsActive = wallet.Status == MomoPayment.Common.Enums.WalletStatus.Active
        };
    }
}
