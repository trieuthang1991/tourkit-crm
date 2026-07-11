using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>Hạng phòng KS (legacy class_hotel) dưới /api/v1/room-classes — dùng quyền đặt dịch vụ lẻ.</summary>
[ApiController]
[Route("api/v1/room-classes")]
public sealed class RoomClassesController(IRoomClassService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.ServiceBookingView)]
    public async Task<IActionResult> List() => Ok(await service.ListAsync());

    [HttpPost]
    [Authorize(Permissions.ServiceBookingManage)]
    public async Task<IActionResult> Create([FromBody] CreateRoomClassDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/room-classes/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.ServiceBookingManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoomClassDto dto)
    {
        await service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.ServiceBookingManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
