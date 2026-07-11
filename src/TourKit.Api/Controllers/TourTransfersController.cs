using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>Chuyển chuyến cho đơn (legacy TransferHistory) dưới /api/v1/orders/{orderId}/transfers.</summary>
[ApiController]
[Route("api/v1/orders/{orderId:guid}/transfers")]
public sealed class TourTransfersController(ITourTransferService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.BookingView)]
    public async Task<IActionResult> List(Guid orderId) => Ok(await service.ListByOrderAsync(orderId));

    [HttpPost]
    [Authorize(Permissions.BookingCreate)]
    public async Task<IActionResult> Transfer(Guid orderId, [FromBody] TransferOrderDto dto)
    {
        var created = await service.TransferAsync(orderId, dto);
        return Created($"/api/v1/orders/{orderId}/transfers/{created.Id}", created);
    }
}
