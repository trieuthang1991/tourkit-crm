using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Auth;
using TourKit.Api.Authz;
using TourKit.Application.Finance;
using TourKit.Application.Finance.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>
/// Duyệt phiếu chi NHIỀU CẤP (đối xứng ReceiptApprovalsController) — ALTERNATIVE cho duyệt 1 cấp
/// (POST /payments/{id}/approve vẫn giữ nguyên).
/// </summary>
[ApiController]
[Route("api/v1/payments/{paymentId:guid}/approval")]
public sealed class PaymentApprovalsController(IPaymentApprovalService service, ICurrentUser currentUser) : ControllerBase
{
    [HttpPost]
    [Authorize(Permissions.PaymentApprovalStart)]
    public async Task<IActionResult> Start(Guid paymentId, [FromBody] StartApprovalDto dto)
    {
        var created = await service.StartAsync(paymentId, dto);
        return Created($"/api/v1/payments/{paymentId}/approval", created);
    }

    [HttpPost("act")]
    [Authorize(Permissions.PaymentApprovalAct)]
    public async Task<IActionResult> Act(Guid paymentId, [FromBody] ActApprovalDto dto)
    {
        if (currentUser.UserId is null)
        {
            return Unauthorized();
        }

        var updated = await service.ActAsync(paymentId, currentUser.UserId.Value, dto);
        return Ok(updated);
    }

    [HttpGet]
    [Authorize(Permissions.PaymentView)]
    public async Task<IActionResult> Get(Guid paymentId)
    {
        var approval = await service.GetAsync(paymentId);
        return Ok(approval);
    }
}
