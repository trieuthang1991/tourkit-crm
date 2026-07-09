using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Api.Auth;
using TourKit.Application.Finance;
using TourKit.Application.Finance.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>
/// Duyệt phiếu thu NHIỀU CẤP (legacy ReceiptVoucherApproval + ReceiptVoucherApprovalStepUser) —
/// ALTERNATIVE cho duyệt 1 cấp ở <see cref="ReceiptsController"/> (POST /receipts/{id}/approve vẫn giữ nguyên).
/// </summary>
[ApiController]
[Route("api/v1/receipts/{receiptId:guid}/approval")]
public sealed class ReceiptApprovalsController(IReceiptApprovalService service, ICurrentUser currentUser) : ControllerBase
{
    [HttpPost]
    [Authorize(Permissions.ReceiptApprovalStart)]
    public async Task<IActionResult> Start(Guid receiptId, [FromBody] StartApprovalDto dto)
    {
        var created = await service.StartAsync(receiptId, dto);
        return Created($"/api/v1/receipts/{receiptId}/approval", created);
    }

    // Acting user lấy từ ICurrentUser ở controller (không phải service) để giữ nguyên 401 khi thiếu current
    // user (service chỉ nhận UserId đã xác định, ném ForbiddenException (→403) cho case không phải người
    // duyệt hợp lệ ở bước hiện tại).
    [HttpPost("act")]
    [Authorize(Permissions.ReceiptApprovalAct)]
    public async Task<IActionResult> Act(Guid receiptId, [FromBody] ActApprovalDto dto)
    {
        if (currentUser.UserId is null)
        {
            return Unauthorized();
        }

        var updated = await service.ActAsync(receiptId, currentUser.UserId.Value, dto);
        return Ok(updated);
    }

    [HttpGet]
    [Authorize(Permissions.ReceiptView)]
    public async Task<IActionResult> Get(Guid receiptId)
    {
        var approval = await service.GetAsync(receiptId);
        return Ok(approval);
    }
}
