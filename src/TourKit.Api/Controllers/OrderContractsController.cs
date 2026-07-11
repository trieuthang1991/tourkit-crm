using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Booking;

namespace TourKit.Api.Controllers;

/// <summary>Dữ liệu in hợp đồng tour (legacy contract_tour) dưới /api/v1/orders/{orderId}/contract.</summary>
[ApiController]
[Route("api/v1/orders/{orderId:guid}/contract")]
public sealed class OrderContractsController(IOrderContractService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.BookingView)]
    public async Task<IActionResult> Get(Guid orderId) => Ok(await service.GetAsync(orderId));
}
