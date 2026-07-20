using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Billing;
using TourKit.Application.Billing.Dtos;

namespace TourKit.Api.Pages.Billing;

[Authorize(Policy = "subscription.view")]
public class IndexModel : PageModel
{
    private readonly IBillingService _svc;
    public IndexModel(IBillingService svc) => _svc = svc;

    public IReadOnlyList<PlanDto> Plans { get; private set; } = [];
    public SubscriptionDto? Subscription { get; private set; }

    public async Task OnGetAsync()
    {
        Plans = await _svc.ListPlansAsync();
        Subscription = await _svc.GetSubscriptionAsync();
    }

    public async Task<IActionResult> OnPostChangePlanAsync(string planCode)
    {
        await _svc.ChangePlanAsync(new ChangePlanDto(planCode));
        TempData["ok"] = "Đã đổi gói dịch vụ.";
        return RedirectToPage();
    }
}
