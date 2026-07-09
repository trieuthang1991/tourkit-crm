using TourKit.Application.Billing.Dtos;

namespace TourKit.Application.Billing;

public interface IBillingService
{
    Task<IReadOnlyList<PlanDto>> ListPlansAsync();
    Task<SubscriptionDto> GetSubscriptionAsync();
    Task<SubscriptionDto> ChangePlanAsync(ChangePlanDto dto);
}
