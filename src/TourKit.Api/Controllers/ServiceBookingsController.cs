using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;
using TourKit.Shared.Enums;

namespace TourKit.Api.Controllers;

/// <summary>Đặt dịch vụ lẻ (hotel/vé/visa...) dưới /api/v1/service-bookings.</summary>
[ApiController]
[Route("api/v1/service-bookings")]
public sealed class ServiceBookingsController(IServiceBookingService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.ServiceBookingView)]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int size = 20,
        [FromQuery] ServiceBookingType? type = null,
        [FromQuery] Guid? orderId = null)
    {
        var result = await service.ListAsync(page, size, type, orderId);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Permissions.ServiceBookingManage)]
    public async Task<IActionResult> Create([FromBody] CreateServiceBookingDto dto)
    {
        var created = await service.CreateAsync(dto);
        return Created($"/api/v1/service-bookings/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.ServiceBookingManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServiceBookingDto dto)
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
