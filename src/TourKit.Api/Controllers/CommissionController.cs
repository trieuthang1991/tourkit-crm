using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Commission;
using TourKit.Application.Commission.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>Hoa hồng/chia lợi nhuận theo đơn — dưới /api/v1/orders/{orderId}/profit(-shares).</summary>
[ApiController]
[Route("api/v1/orders/{orderId:guid}")]
public sealed class CommissionController(ICommissionService service) : ControllerBase
{
    [HttpGet("profit")]
    [Authorize(Permissions.CommissionView)]
    public async Task<IActionResult> GetProfit(Guid orderId)
    {
        var profit = await service.GetOrderProfitAsync(orderId);
        return Ok(profit);
    }

    [HttpPost("profit-shares")]
    [Authorize(Permissions.CommissionCreate)]
    public async Task<IActionResult> CreateProfitShare(Guid orderId, [FromBody] CreateProfitShareDto dto)
    {
        var created = await service.CreateProfitShareAsync(orderId, dto);
        return Created($"/api/v1/orders/{orderId}/profit-shares/{created.Id}", created);
    }

    [HttpGet("profit-shares")]
    [Authorize(Permissions.CommissionView)]
    public async Task<IActionResult> ListProfitShares(Guid orderId)
    {
        var shares = await service.ListProfitSharesAsync(orderId);
        return Ok(shares);
    }
}
