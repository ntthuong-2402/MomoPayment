using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MomoPayment.Common.DTOs;
using MomoPayment.Common.Security;
using Notification.API.Data;
using Notification.API.Entities;

namespace Notification.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly NotificationDbContext _dbContext;

    public NotificationsController(NotificationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AppNotification>>>> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var userId = JwtHelper.GetUserId(User);
        if (userId == null) return Unauthorized(ApiResponse.Fail("Không có quyền truy cập", "UNAUTHORIZED"));

        var query = _dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId.Value)
            .OrderByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var pageNum = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 100);

        var items = await query
            .Skip((pageNum - 1) * size)
            .Take(size)
            .ToListAsync(cancellationToken);

        return Ok(ApiResponse<PagedResult<AppNotification>>.Ok(new PagedResult<AppNotification>(items, totalCount, pageNum, size)));
    }

    [HttpGet("unread-count")]
    public async Task<ActionResult<ApiResponse<int>>> GetUnreadCount(CancellationToken cancellationToken)
    {
        var userId = JwtHelper.GetUserId(User);
        if (userId == null) return Unauthorized(ApiResponse.Fail("Không có quyền truy cập", "UNAUTHORIZED"));

        var count = await _dbContext.Notifications
            .CountAsync(n => n.UserId == userId.Value && !n.IsRead, cancellationToken);

        return Ok(ApiResponse<int>.Ok(count));
    }

    [HttpPut("{id:guid}/read")]
    public async Task<ActionResult<ApiResponse>> MarkAsRead([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var userId = JwtHelper.GetUserId(User);
        if (userId == null) return Unauthorized(ApiResponse.Fail("Không có quyền truy cập", "UNAUTHORIZED"));

        var notif = await _dbContext.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId.Value, cancellationToken);

        if (notif == null)
        {
            return NotFound(ApiResponse.Fail("Không tìm thấy thông báo", "NOT_FOUND"));
        }

        notif.IsRead = true;
        notif.ReadAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(ApiResponse.Ok("Đã đánh dấu đã đọc thông báo"));
    }

    [HttpPut("read-all")]
    public async Task<ActionResult<ApiResponse>> MarkAllAsRead(CancellationToken cancellationToken)
    {
        var userId = JwtHelper.GetUserId(User);
        if (userId == null) return Unauthorized(ApiResponse.Fail("Không có quyền truy cập", "UNAUTHORIZED"));

        await _dbContext.Notifications
            .Where(n => n.UserId == userId.Value && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, DateTime.UtcNow), cancellationToken);

        return Ok(ApiResponse.Ok("Đã đánh dấu tất cả thông báo là đã đọc"));
    }
}
