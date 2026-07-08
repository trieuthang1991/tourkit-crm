using Microsoft.EntityFrameworkCore;
using TourKit.Api.Authz;
using TourKit.Infrastructure.Persistence;

namespace TourKit.Api.Billing;

/// <summary>
/// REST endpoints cho Plan/Subscription dưới /api/v1/plans và /api/v1/subscription.
/// Endpoint mỏng: validate → thao tác DbContext → map DTO (conventions §6).
/// Enforce hết hạn: SubscriptionGuardMiddleware chặn endpoint nghiệp vụ khi subscription Expired/Cancelled/quá hạn.
/// </summary>
public static class BillingEndpoints
{
    public static IEndpointRouteBuilder MapBillingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/plans", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.Plans.AsNoTracking()
                .OrderBy(p => p.PriceMonthly)
                .Select(p => new PlanResponse(p.Id, p.Code, p.Name, p.MaxUsers, p.MaxTours, p.PriceMonthly))
                .ToListAsync(ct))).RequireAuthorization(Permissions.SubscriptionView);

        app.MapGet("/api/v1/subscription", async (AppDbContext db, CancellationToken ct) =>
        {
            var response = await (
                from s in db.Subscriptions.AsNoTracking()
                join p in db.Plans.AsNoTracking() on s.PlanId equals p.Id
                select new SubscriptionResponse(s.Id, s.PlanId, p.Code, s.Status, s.StartedAt, s.ExpiresAt))
                .FirstOrDefaultAsync(ct);
            return response is null ? Results.NotFound() : Results.Ok(response);
        }).RequireAuthorization(Permissions.SubscriptionView);

        app.MapPost("/api/v1/subscription/change-plan", async (ChangePlanRequest body, AppDbContext db, CancellationToken ct) =>
        {
            var plan = await db.Plans.AsNoTracking().FirstOrDefaultAsync(p => p.Code == body.PlanCode, ct);
            if (plan is null)
            {
                return ValidationError("PlanCode không hợp lệ.");
            }

            var subscription = await db.Subscriptions.FirstOrDefaultAsync(ct);
            if (subscription is null)
            {
                return Results.NotFound();
            }

            subscription.PlanId = plan.Id;
            await db.SaveChangesAsync(ct);

            var response = new SubscriptionResponse(
                subscription.Id, subscription.PlanId, plan.Code, subscription.Status,
                subscription.StartedAt, subscription.ExpiresAt);
            return Results.Ok(response);
        }).RequireAuthorization(Permissions.SubscriptionManage);

        return app;
    }

    // Validation tối thiểu cho foundation; Phase sau thay bằng FluentValidation (conventions §6).
    private static IResult ValidationError(string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { ["Request"] = [message] });
}
