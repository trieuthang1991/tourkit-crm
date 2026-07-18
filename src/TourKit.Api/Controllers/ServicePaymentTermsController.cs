using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>Lịch thanh toán NCC theo booking dịch vụ dưới /api/v1/service-bookings/{bookingId}/payment-terms.</summary>
[ApiController]
[Route("api/v1/service-bookings/{bookingId:guid}/payment-terms")]
public sealed class ServicePaymentTermsController(IServicePaymentTermService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.ServiceBookingView)]
    public async Task<IActionResult> List(Guid bookingId)
        => Ok(await service.ListByBookingAsync(bookingId));

    [HttpPost]
    [Authorize(Permissions.ServiceBookingManage)]
    public async Task<IActionResult> Create(Guid bookingId, [FromBody] CreateServicePaymentTermDto dto)
    {
        var created = await service.CreateAsync(bookingId, dto);
        return Created($"/api/v1/service-bookings/{bookingId}/payment-terms/{created.Id}", created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.ServiceBookingManage)]
    public async Task<IActionResult> Update(Guid bookingId, Guid id, [FromBody] UpdateServicePaymentTermDto dto)
    {
        await service.UpdateAsync(bookingId, id, dto);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.ServiceBookingManage)]
    public async Task<IActionResult> Delete(Guid bookingId, Guid id)
    {
        await service.DeleteAsync(bookingId, id);
        return NoContent();
    }
}
