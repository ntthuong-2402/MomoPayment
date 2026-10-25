using Identity.API.DTOs;
using Identity.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MomoPayment.Common.DTOs;
using MomoPayment.Common.Security;

namespace Identity.API.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IIdentityService _identityService;

    public UsersController(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserResponse>>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = JwtHelper.GetUserId(User);
        if (userId == null)
        {
            return Unauthorized(ApiResponse.Fail("Không thể xác định danh tính", "UNAUTHORIZED"));
        }

        var result = await _identityService.GetUserProfileAsync(userId.Value, cancellationToken);
        return Ok(ApiResponse<UserResponse>.Ok(result));
    }

    [HttpGet("lookup")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserLookupResponse>>> LookupByPhone([FromQuery] string phone, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return BadRequest(ApiResponse.Fail("Số điện thoại không được để trống", "PHONE_REQUIRED"));
        }

        var result = await _identityService.LookupUserByPhoneAsync(phone, cancellationToken);
        return Ok(ApiResponse<UserLookupResponse>.Ok(result, "Tìm thấy thông tin người nhận"));
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserResponse>>> GetUserById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _identityService.GetUserProfileAsync(id, cancellationToken);
        return Ok(ApiResponse<UserResponse>.Ok(result));
    }
}
