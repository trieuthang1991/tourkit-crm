using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Audit;

namespace TourKit.Api.Controllers;

/// <summary>REST đọc nhật ký thao tác (audit) dưới /api/v1/activity-logs — chỉ xem.</summary>
[ApiController]
[Route("api/v1/activity-logs")]
public sealed class ActivityLogsController(IActivityLogService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.ActivityLogView)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        [FromQuery] string? entityName = null,
        [FromQuery] string? entityId = null)
    {
        var result = await service.ListAsync(page, size, entityName, entityId);
        return Ok(result);
    }
}
