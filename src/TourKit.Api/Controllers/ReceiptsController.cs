using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Finance;
using TourKit.Application.Finance.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>Phiếu thu (duyệt 1 cấp) + công nợ phải thu theo đơn.</summary>
[ApiController]
[Route("api/v1")]
public sealed class ReceiptsController(IReceiptService service) : ControllerBase
{
    [HttpPost("orders/{orderId:guid}/receipts")]
    [Authorize(Permissions.ReceiptCreate)]
    public async Task<IActionResult> Create(Guid orderId, [FromBody] CreateReceiptDto dto)
    {
        var created = await service.CreateAsync(orderId, dto);
        return Created($"/api/v1/orders/{orderId}/receipts/{created.Id}", created);
    }

    // Duyệt phiếu → ghi nhận dòng tiền (mới tính vào công nợ). Mode 1 cấp (Default).
    [HttpPost("receipts/{receiptId:guid}/approve")]
    [Authorize(Permissions.ReceiptApprove)]
    public async Task<IActionResult> Approve(Guid receiptId)
    {
        var updated = await service.ApproveAsync(receiptId);
        return Ok(updated);
    }

    // Không duyệt (từ chối) → không ghi nhận.
    [HttpPost("receipts/{receiptId:guid}/reject")]
    [Authorize(Permissions.ReceiptApprove)]
    public async Task<IActionResult> Reject(Guid receiptId)
    {
        var updated = await service.RejectAsync(receiptId);
        return Ok(updated);
    }

    [HttpGet("orders/{orderId:guid}/receipts")]
    [Authorize(Permissions.ReceiptView)]
    public async Task<IActionResult> ListByOrder(Guid orderId)
    {
        var receipts = await service.ListByOrderAsync(orderId);
        return Ok(receipts);
    }

    // Danh sách phiếu thu TỔNG (toàn tenant) — trang "Phiếu thu" độc lập.
    [HttpGet("receipts")]
    [Authorize(Permissions.ReceiptView)]
    public async Task<IActionResult> ListAll([FromQuery] int page = 1, [FromQuery] int size = 20)
        => Ok(await service.ListAllAsync(page, size));

    [HttpGet("orders/{orderId:guid}/balance")]
    [Authorize(Permissions.ReceiptView)]
    public async Task<IActionResult> GetBalance(Guid orderId)
    {
        var balance = await service.GetBalanceAsync(orderId);
        return Ok(balance);
    }
}
