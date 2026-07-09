using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>REST endpoints cho Vehicle (xe) dưới /api/v1/vehicles.</summary>
[ApiController]
[Route("api/v1/vehicles")]
public sealed class VehiclesController(IVehicleService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.VehicleView)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int size = 20)
    {
        var result = await service.ListAsync(page, size);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Permissions.VehicleManage)]
    public async Task<IActionResult> Create([FromBody] CreateVehicleDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/vehicles/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.VehicleManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVehicleDto dto)
    {
        await service.UpdateAsync(id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.VehicleManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        await service.DeleteAsync(id);
        return NoContent();
    }
}
