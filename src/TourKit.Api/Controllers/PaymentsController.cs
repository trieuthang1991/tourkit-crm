using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Finance;
using TourKit.Application.Finance.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>Phiếu chi (đối xứng phiếu thu) — chi trả cho NCC theo đơn.</summary>
[ApiController]
[Route("api/v1")]
public sealed class PaymentsController(IPaymentService service) : ControllerBase
{
    [HttpPost("orders/{orderId:guid}/payments")]
    [Authorize(Permissions.PaymentCreate)]
    public async Task<IActionResult> Create(Guid orderId, [FromBody] CreatePaymentDto dto)
    {
        var created = await service.CreateAsync(orderId, dto);
        return Created($"/api/v1/orders/{orderId}/payments/{created.Id}", created);
    }

    [HttpPost("payments/{paymentId:guid}/approve")]
    [Authorize(Permissions.PaymentApprove)]
    public async Task<IActionResult> Approve(Guid paymentId)
    {
        var updated = await service.ApproveAsync(paymentId);
        return Ok(updated);
    }

    [HttpPost("payments/{paymentId:guid}/reject")]
    [Authorize(Permissions.PaymentApprove)]
    public async Task<IActionResult> Reject(Guid paymentId)
    {
        var updated = await service.RejectAsync(paymentId);
        return Ok(updated);
    }

    [HttpGet("orders/{orderId:guid}/payments")]
    [Authorize(Permissions.PaymentView)]
    public async Task<IActionResult> ListByOrder(Guid orderId)
    {
        var payments = await service.ListByOrderAsync(orderId);
        return Ok(payments);
    }

    // Danh sách phiếu chi TỔNG (toàn tenant) — trang "Phiếu chi" độc lập.
    [HttpGet("payments")]
    [Authorize(Permissions.PaymentView)]
    public async Task<IActionResult> ListAll(
        [FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] PaymentListFilter? filter = null)
        => Ok(await service.ListAllAsync(page, size, filter));

    // Thẻ thống kê đầu màn Phiếu chi: tổng + tiền + đếm theo trạng thái.
    [HttpGet("payments/stats")]
    [Authorize(Permissions.PaymentView)]
    public async Task<IActionResult> PaymentStats() => Ok(await service.GetStatsAsync());
}
