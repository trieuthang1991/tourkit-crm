using FluentValidation;
using TourKit.Application.Billing.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Billing;

/// <summary>
/// Plan (catalog global) &amp; Subscription (per-tenant, MVP một subscription/tenant — global query filter
/// tự lọc theo tenant hiện tại). Enforce hết hạn nằm ở <c>SubscriptionGuardMiddleware</c> (Api layer, giữ nguyên).
/// </summary>
public sealed class BillingService(
    IRepository<Plan> planRepo,
    IRepository<Subscription> subscriptionRepo,
    IValidator<ChangePlanDto> changePlanValidator) : IBillingService
{
    public async Task<IReadOnlyList<PlanDto>> ListPlansAsync()
    {
        var plans = await planRepo.ListAsync();
        return plans.OrderBy(p => p.PriceMonthly).Select(Map).ToList();
    }

    public async Task<SubscriptionDto> GetSubscriptionAsync()
    {
        var subscription = FirstOrDefault(await subscriptionRepo.ListAsync());
        if (subscription is null)
        {
            throw new NotFoundException();
        }

        var plan = await planRepo.GetByIdAsync(subscription.PlanId);
        if (plan is null)
        {
            throw new NotFoundException();
        }

        return Map(subscription, plan.Code);
    }

    public async Task<SubscriptionDto> ChangePlanAsync(ChangePlanDto dto)
    {
        await Validate(changePlanValidator, dto);

        var plan = FirstOrDefault(await planRepo.ListAsync(p => p.Code == dto.PlanCode));
        if (plan is null)
        {
            throw new ValidationAppException("Gói không tồn tại.");
        }

        var subscription = FirstOrDefault(await subscriptionRepo.ListAsync());
        if (subscription is null)
        {
            throw new NotFoundException();
        }

        subscription.PlanId = plan.Id;
        subscriptionRepo.Update(subscription);
        await subscriptionRepo.SaveChangesAsync();

        return Map(subscription, plan.Code);
    }

    private static T? FirstOrDefault<T>(IReadOnlyList<T> items) where T : class => items.Count > 0 ? items[0] : null;

    private static async Task Validate<T>(IValidator<T> validator, T dto)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }
    }

    private static PlanDto Map(Plan p) => new(p.Id, p.Code, p.Name, p.MaxUsers, p.MaxTours, p.PriceMonthly);

    private static SubscriptionDto Map(Subscription s, string planCode) => new(
        s.Id, s.PlanId, planCode, s.Status, s.StartedAt, s.ExpiresAt);
}
