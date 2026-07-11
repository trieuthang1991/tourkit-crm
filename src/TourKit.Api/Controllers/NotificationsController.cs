using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Application.Notifications;

namespace TourKit.Api.Controllers;

/// <summary>Thông báo in-app của user hiện tại dưới /api/v1/notifications. Chỉ cần đăng nhập (thông báo cá nhân).</summary>
[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public sealed class NotificationsController(INotificationService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] bool unreadOnly = false)
        => Ok(await service.ListMineAsync(unreadOnly));

    [HttpGet("unread-count")]
    public async Task<IActionResult> UnreadCount() => Ok(new { count = await service.UnreadCountAsync() });

    [HttpPost("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id)
    {
        await service.MarkReadAsync(id);
        return NoContent();
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        await service.MarkAllReadAsync();
        return NoContent();
    }
}
