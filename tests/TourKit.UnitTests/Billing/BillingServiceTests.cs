using TourKit.Application.Billing;
using TourKit.Application.Billing.Dtos;
using TourKit.Application.Billing.Validators;
using TourKit.Application.Common;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.UnitTests.Billing;

/// <summary>Test <see cref="BillingService"/> qua fake <see cref="IRepository{T}"/> in-memory.</summary>
public class BillingServiceTests
{
    private static BillingService NewService(out FakeRepository<Plan> planRepo, out FakeRepository<Subscription> subscriptionRepo)
    {
        planRepo = new FakeRepository<Plan>();
        subscriptionRepo = new FakeRepository<Subscription>();
        return new BillingService(planRepo, subscriptionRepo, new ChangePlanValidator());
    }

    private static async Task<Plan> SeedPlanAsync(FakeRepository<Plan> planRepo, string code, decimal price)
    {
        var plan = new Plan { Code = code, Name = code, MaxUsers = 3, MaxTours = 10, PriceMonthly = price };
        await planRepo.AddAsync(plan);
        await planRepo.SaveChangesAsync();
        return plan;
    }

    private static async Task<Subscription> SeedSubscriptionAsync(FakeRepository<Subscription> subscriptionRepo, Guid planId)
    {
        var subscription = new Subscription
        {
            PlanId = planId, Status = SubscriptionStatus.Active, StartedAt = DateTimeOffset.UtcNow,
        };
        await subscriptionRepo.AddAsync(subscription);
        await subscriptionRepo.SaveChangesAsync();
        return subscription;
    }

    [Fact]
    public async Task ListPlansAsync_returns_plans_ordered_by_price()
    {
        var service = NewService(out var planRepo, out _);
        await SeedPlanAsync(planRepo, "pro", 990_000m);
        await SeedPlanAsync(planRepo, "free", 0m);

        var plans = await service.ListPlansAsync();

        Assert.Equal(["free", "pro"], plans.Select(p => p.Code));
    }

    [Fact]
    public async Task GetSubscriptionAsync_returns_current_tenant_subscription()
    {
        var service = NewService(out var planRepo, out var subscriptionRepo);
        var plan = await SeedPlanAsync(planRepo, "free", 0m);
        await SeedSubscriptionAsync(subscriptionRepo, plan.Id);

        var subscription = await service.GetSubscriptionAsync();

        Assert.Equal("free", subscription.PlanCode);
        Assert.Equal(SubscriptionStatus.Active, subscription.Status);
    }

    [Fact]
    public async Task GetSubscriptionAsync_no_subscription_throws_NotFoundException()
    {
        var service = NewService(out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetSubscriptionAsync());
    }

    [Fact]
    public async Task ChangePlanAsync_updates_subscription_plan()
    {
        var service = NewService(out var planRepo, out var subscriptionRepo);
        var free = await SeedPlanAsync(planRepo, "free", 0m);
        var pro = await SeedPlanAsync(planRepo, "pro", 990_000m);
        await SeedSubscriptionAsync(subscriptionRepo, free.Id);

        var updated = await service.ChangePlanAsync(new ChangePlanDto("pro"));

        Assert.Equal("pro", updated.PlanCode);
        Assert.Equal(pro.Id, updated.PlanId);
    }

    [Fact]
    public async Task ChangePlanAsync_unknown_plan_code_throws_ValidationAppException()
    {
        var service = NewService(out var planRepo, out var subscriptionRepo);
        var free = await SeedPlanAsync(planRepo, "free", 0m);
        await SeedSubscriptionAsync(subscriptionRepo, free.Id);

        await Assert.ThrowsAsync<ValidationAppException>(
            () => service.ChangePlanAsync(new ChangePlanDto("nonexistent")));
    }

    [Fact]
    public async Task ChangePlanAsync_empty_plan_code_throws_ValidationAppException()
    {
        var service = NewService(out _, out _);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.ChangePlanAsync(new ChangePlanDto("")));
    }

    [Fact]
    public async Task ChangePlanAsync_no_subscription_throws_NotFoundException()
    {
        var service = NewService(out var planRepo, out _);
        await SeedPlanAsync(planRepo, "pro", 990_000m);

        await Assert.ThrowsAsync<NotFoundException>(() => service.ChangePlanAsync(new ChangePlanDto("pro")));
    }
}
