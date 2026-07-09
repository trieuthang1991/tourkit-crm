using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Billing;
using TourKit.Application.Billing.Dtos;

namespace TourKit.Api.Controllers;

/// <summary>
/// Plan/Subscription — /api/v1/plans và /api/v1/subscription.
/// Enforce hết hạn: SubscriptionGuardMiddleware chặn endpoint nghiệp vụ khi subscription Expired/Cancelled/quá hạn.
/// </summary>
[ApiController]
[Route("api/v1")]
public sealed class BillingController(IBillingService service) : ControllerBase
{
    [HttpGet("plans")]
    [Authorize(Permissions.SubscriptionView)]
    public async Task<IActionResult> ListPlans()
    {
        var plans = await service.ListPlansAsync();
        return Ok(plans);
    }

    [HttpGet("subscription")]
    [Authorize(Permissions.SubscriptionView)]
    public async Task<IActionResult> GetSubscription()
    {
        var subscription = await service.GetSubscriptionAsync();
        return Ok(subscription);
    }

    [HttpPost("subscription/change-plan")]
    [Authorize(Permissions.SubscriptionManage)]
    public async Task<IActionResult> ChangePlan([FromBody] ChangePlanDto dto)
    {
        var subscription = await service.ChangePlanAsync(dto);
        return Ok(subscription);
    }
}
