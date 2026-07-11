using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>REST endpoints phân xe cho chuyến dưới /api/v1/vehicle-assignments.</summary>
[ApiController]
[Route("api/v1/vehicle-assignments")]
public sealed class VehicleAssignmentsController(IVehicleAssignmentService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.VehicleView)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        [FromQuery] Guid? departureId = null)
    {
        var result = await service.ListAsync(page, size, departureId);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Permissions.VehicleManage)]
    public async Task<IActionResult> Create([FromBody] CreateVehicleAssignmentDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/vehicle-assignments/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.VehicleManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateVehicleAssignmentDto dto)
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
