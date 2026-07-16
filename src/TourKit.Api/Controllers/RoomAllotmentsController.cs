using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Rooms;
using TourKit.Application.Rooms.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>Quỹ phòng / allotment (legacy "QUỸ PHÒNG KHÁCH SẠN") dưới /api/v1/room-allotments — tồn + giá NET theo ngày.</summary>
[ApiController]
[Route("api/v1/room-allotments")]
public sealed class RoomAllotmentsController(IRoomAllotmentService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.RoomFundView)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] RoomAllotmentListFilter? filter = null)
        => Ok(await service.ListAsync(page, size, filter));

    [HttpGet("stats")]
    [Authorize(Permissions.RoomFundView)]
    public async Task<IActionResult> Stats([FromQuery] RoomAllotmentListFilter? filter = null)
        => Ok(await service.GetStatsAsync(filter));

    [HttpGet("{id:guid}")]
    [Authorize(Permissions.RoomFundView)]
    public async Task<IActionResult> Get(Guid id) => Ok(await service.GetAsync(id));

    [HttpPost]
    [Authorize(Permissions.RoomFundManage)]
    public async Task<IActionResult> Create([FromBody] CreateRoomAllotmentDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/room-allotments/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.RoomFundManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoomAllotmentDto dto)
    {
        await service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.RoomFundManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
