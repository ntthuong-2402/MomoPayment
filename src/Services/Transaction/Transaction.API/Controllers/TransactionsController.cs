using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MomoPayment.Common.DTOs;
using MomoPayment.Common.Security;
using Transaction.API.DTOs;
using Transaction.API.Services;

namespace Transaction.API.Controllers;

[ApiController]
[Route("api/transactions")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionService _transactionService;

    public TransactionsController(ITransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    [HttpPost("transfer")]
    public async Task<ActionResult<ApiResponse<TransactionResponse>>> Transfer(
        [FromBody] TransferRequest request,
        [FromHeader(Name = "X-Idempotency-Key")] string? headerIdempotencyKey,
        CancellationToken cancellationToken)
    {
        var userId = JwtHelper.GetUserId(User);
        var userPhone = JwtHelper.GetPhoneNumber(User);

        if (userId == null || string.IsNullOrEmpty(userPhone))
        {
            return Unauthorized(ApiResponse.Fail("Không thể xác định thông tin người dùng", "UNAUTHORIZED"));
        }

        // Header takes precedence over body
        if (!string.IsNullOrWhiteSpace(headerIdempotencyKey))
        {
            request.IdempotencyKey = headerIdempotencyKey;
        }

        var result = await _transactionService.TransferMoneyAsync(userId.Value, userPhone, request, cancellationToken);
        return Ok(ApiResponse<TransactionResponse>.Ok(result, $"Chuyển thành công {result.Amount:N0} VND đến {result.ReceiverPhone}"));
    }

    [HttpPost("payment")]
    public async Task<ActionResult<ApiResponse<TransactionResponse>>> Payment(
        [FromBody] PaymentRequest request,
        [FromHeader(Name = "X-Idempotency-Key")] string? headerIdempotencyKey,
        CancellationToken cancellationToken)
    {
        var userId = JwtHelper.GetUserId(User);
        var userPhone = JwtHelper.GetPhoneNumber(User);

        if (userId == null || string.IsNullOrEmpty(userPhone))
        {
            return Unauthorized(ApiResponse.Fail("Không thể xác định thông tin người dùng", "UNAUTHORIZED"));
        }

        if (!string.IsNullOrWhiteSpace(headerIdempotencyKey))
        {
            request.IdempotencyKey = headerIdempotencyKey;
        }

        var result = await _transactionService.MakePaymentAsync(userId.Value, userPhone, request, cancellationToken);
        return Ok(ApiResponse<TransactionResponse>.Ok(result, $"Thanh toán thành công {result.Amount:N0} VND"));
    }

    [HttpGet("history")]
    public async Task<ActionResult<ApiResponse<PagedResult<TransactionResponse>>>> GetHistory(
        [FromQuery] TransactionFilterRequest filter,
        CancellationToken cancellationToken)
    {
        var userId = JwtHelper.GetUserId(User);
        if (userId == null)
        {
            return Unauthorized(ApiResponse.Fail("Không thể xác định thông tin người dùng", "UNAUTHORIZED"));
        }

        var result = await _transactionService.GetHistoryAsync(userId.Value, filter, cancellationToken);
        return Ok(ApiResponse<PagedResult<TransactionResponse>>.Ok(result));
    }

    [HttpGet("code/{code}")]
    public async Task<ActionResult<ApiResponse<TransactionResponse>>> GetByCode(
        [FromRoute] string code,
        CancellationToken cancellationToken)
    {
        var userId = JwtHelper.GetUserId(User);
        if (userId == null)
        {
            return Unauthorized(ApiResponse.Fail("Không thể xác định thông tin người dùng", "UNAUTHORIZED"));
        }

        var result = await _transactionService.GetTransactionByCodeAsync(code, userId.Value, cancellationToken);
        return Ok(ApiResponse<TransactionResponse>.Ok(result));
    }
}
