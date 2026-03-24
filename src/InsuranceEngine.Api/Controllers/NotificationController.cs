using InsuranceEngine.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InsuranceEngine.Api.Controllers;

/// <summary>In-app notifications for workflow events.</summary>
[ApiController]
[Route("api/notifications")]
[Produces("application/json")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>Get unread notifications for the current user.</summary>
    [HttpGet]
    public async Task<IActionResult> GetUnread(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var userId = User.Identity?.Name ?? string.Empty;
        if (string.IsNullOrEmpty(userId))
            return Ok(new { data = Array.Empty<object>(), totalCount = 0, page, pageSize });

        pageSize = Math.Min(Math.Max(pageSize, 1), 100);
        page = Math.Max(page, 1);

        var (notifications, totalCount) = await _notificationService.GetUnread(userId, page, pageSize);
        return Ok(new
        {
            data = notifications.Select(n => new
            {
                n.Id,
                n.Message,
                n.RelatedModule,
                n.RelatedId,
                n.IsRead,
                n.CreatedAt
            }),
            totalCount,
            page,
            pageSize
        });
    }

    /// <summary>Mark a specific notification as read.</summary>
    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = User.Identity?.Name ?? string.Empty;
        if (string.IsNullOrEmpty(userId))
            return BadRequest(new { error = "User not identified." });

        await _notificationService.MarkAsRead(id, userId);
        return Ok(new { success = true });
    }

    /// <summary>Mark all notifications as read for the current user.</summary>
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = User.Identity?.Name ?? string.Empty;
        if (string.IsNullOrEmpty(userId))
            return BadRequest(new { error = "User not identified." });

        await _notificationService.MarkAllAsRead(userId);
        return Ok(new { success = true });
    }
}
