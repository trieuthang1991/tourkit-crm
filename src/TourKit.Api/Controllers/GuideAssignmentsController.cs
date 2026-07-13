using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>REST endpoints phân công HDV cho chuyến dưới /api/v1/guide-assignments.</summary>
[ApiController]
[Route("api/v1/guide-assignments")]
public sealed class GuideAssignmentsController(IGuideAssignmentService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.GuideView)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] GuideAssignmentListFilter? filter = null)
    {
        var result = await service.ListAsync(page, size, filter);
        return Ok(result);
    }

    [HttpGet("stats")]
    [Authorize(Permissions.GuideView)]
    public async Task<IActionResult> Stats() => Ok(await service.GetStatsAsync());

    [HttpPost]
    [Authorize(Permissions.GuideManage)]
    public async Task<IActionResult> Create([FromBody] CreateGuideAssignmentDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/guide-assignments/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.GuideManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateGuideAssignmentDto dto)
    {
        await service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpPost("{id:guid}/handover")]
    [Authorize(Permissions.GuideManage)]
    public async Task<IActionResult> Handover(Guid id, [FromBody] HandoverDto dto)
        => Ok(await service.HandoverAsync(id, dto));

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.GuideManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
