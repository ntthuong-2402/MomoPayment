using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MomoPayment.Caching;
using MomoPayment.Common.DTOs;
using MomoPayment.Common.Exceptions;
using MomoPayment.Common.Security;
using Wallet.API.DTOs;
using Wallet.API.Services;

namespace Wallet.API.Controllers;

[ApiController]
[Route("api/wallet")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly IWalletService _walletService;
    private readonly IIdempotencyService _idempotencyService;

    public WalletController(IWalletService walletService, IIdempotencyService idempotencyService)
    {
        _walletService = walletService;
        _idempotencyService = idempotencyService;
    }

    [HttpGet("balance")]
    public async Task<ActionResult<ApiResponse<WalletResponse>>> GetBalance(CancellationToken cancellationToken)
    {
        var userId = JwtHelper.GetUserId(User);
        if (userId == null) return Unauthorized(ApiResponse.Fail("Không có quyền truy cập", "UNAUTHORIZED"));

        var phone = JwtHelper.GetPhoneNumber(User) ?? string.Empty;
        var result = await _walletService.GetOrCreateWalletAsync(userId.Value, phone, cancellationToken);
        return Ok(ApiResponse<WalletResponse>.Ok(result, "Lấy số dư ví thành công"));
    }

    [HttpPost("deposit")]
    public async Task<ActionResult<ApiResponse<WalletResponse>>> Deposit(
        [FromBody] DepositRequest request,
        [FromHeader(Name = "X-Idempotency-Key")] string? headerIdempotencyKey,
        CancellationToken cancellationToken)
    {
        var userId = JwtHelper.GetUserId(User);
        if (userId == null) return Unauthorized(ApiResponse.Fail("Không có quyền truy cập", "UNAUTHORIZED"));

        var idempotencyKey = headerIdempotencyKey;
        bool keyAcquired = false;

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var cached = await _idempotencyService.GetResultAsync<WalletResponse>(idempotencyKey);
            if (cached != null)
            {
                return Ok(ApiResponse<WalletResponse>.Ok(cached, $"Nạp thành công {cached.Balance:N0} VND vào ví (Idempotent)"));
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
            var result = await _walletService.DepositAsync(userId.Value, request, null, cancellationToken);

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                await _idempotencyService.SaveResultAsync(idempotencyKey, result, TimeSpan.FromHours(24));
            }

            return Ok(ApiResponse<WalletResponse>.Ok(result, $"Nạp thành công {request.Amount:N0} VND vào ví"));
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

    [HttpPost("withdraw")]
    public async Task<ActionResult<ApiResponse<WalletResponse>>> Withdraw(
        [FromBody] WithdrawRequest request,
        [FromHeader(Name = "X-Idempotency-Key")] string? headerIdempotencyKey,
        CancellationToken cancellationToken)
    {
        var userId = JwtHelper.GetUserId(User);
        if (userId == null) return Unauthorized(ApiResponse.Fail("Không có quyền truy cập", "UNAUTHORIZED"));

        var idempotencyKey = headerIdempotencyKey;
        bool keyAcquired = false;

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var cached = await _idempotencyService.GetResultAsync<WalletResponse>(idempotencyKey);
            if (cached != null)
            {
                return Ok(ApiResponse<WalletResponse>.Ok(cached, $"Rút thành công {cached.Balance:N0} VND về ngân hàng (Idempotent)"));
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
            var result = await _walletService.WithdrawAsync(userId.Value, request, null, cancellationToken);

            if (!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                await _idempotencyService.SaveResultAsync(idempotencyKey, result, TimeSpan.FromHours(24));
            }

            return Ok(ApiResponse<WalletResponse>.Ok(result, $"Rút thành công {request.Amount:N0} VND về ngân hàng"));
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

    [HttpGet("ledger")]
    public async Task<ActionResult<ApiResponse<PagedResult<LedgerResponse>>>> GetLedger(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = JwtHelper.GetUserId(User);
        if (userId == null) return Unauthorized(ApiResponse.Fail("Không có quyền truy cập", "UNAUTHORIZED"));

        var result = await _walletService.GetLedgerAsync(userId.Value, Math.Max(1, page), Math.Clamp(pageSize, 1, 100), cancellationToken);
        return Ok(ApiResponse<PagedResult<LedgerResponse>>.Ok(result));
    }
}
