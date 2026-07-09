using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Providers;
using TourKit.Application.Providers.Dtos;

namespace TourKit.Api.Controllers;

[ApiController]
[Route("api/v1/orders/{orderId:guid}/costs")]
public sealed class OrderCostsController(IOrderCostService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.CostView)]
    public async Task<IActionResult> List(Guid orderId)
    {
        var costs = await service.ListByOrderAsync(orderId);
        return Ok(costs);
    }

    [HttpPost]
    [Authorize(Permissions.CostCreate)]
    public async Task<IActionResult> Create(Guid orderId, [FromBody] CreateOrderCostDto dto)
    {
        var created = await service.CreateAsync(orderId, dto);
        return Created($"/api/v1/orders/{orderId}/costs/{created.Id}", created);
    }
}
