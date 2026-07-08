using TourKit.Api.Application;
using TourKit.Api.Authz;
using TourKit.Api.Billing.Features;
using TourKit.Shared.Application;

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
        app.MapGet("/api/v1/plans", async (IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new ListPlansQuery(), ct))
                .Match(p => Results.Ok(p))).RequireAuthorization(Permissions.SubscriptionView);

        app.MapGet("/api/v1/subscription", async (IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new GetSubscriptionQuery(), ct))
                .Match(s => Results.Ok(s))).RequireAuthorization(Permissions.SubscriptionView);

        app.MapPost("/api/v1/subscription/change-plan", async (
            ChangePlanRequest body, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new ChangePlanCommand(body.PlanCode), ct))
                .Match(s => Results.Ok(s))).RequireAuthorization(Permissions.SubscriptionManage);

        return app;
    }
}
