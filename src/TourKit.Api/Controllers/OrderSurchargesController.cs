using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>Phụ thu theo đơn dưới /api/v1/orders/{orderId}/surcharges — cộng vào doanh thu đơn.</summary>
[ApiController]
[Route("api/v1/orders/{orderId:guid}/surcharges")]
public sealed class OrderSurchargesController(IOrderSurchargeService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.BookingView)]
    public async Task<IActionResult> List(Guid orderId) => Ok(await service.ListByOrderAsync(orderId));

    [HttpPost]
    [Authorize(Permissions.BookingCreate)]
    public async Task<IActionResult> Create(Guid orderId, [FromBody] CreateOrderSurchargeDto dto)
    {
        var created = await service.CreateAsync(orderId, dto);
        return Created($"/api/v1/orders/{orderId}/surcharges/{created.Id}", created);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.BookingCreate)]
    public async Task<IActionResult> Delete(Guid orderId, Guid id)
    {
        await service.DeleteAsync(orderId, id);
        return NoContent();
    }
}
