using TourKit.Api.Application;
using TourKit.Api.Authz;
using TourKit.Api.Commission.Features;
using TourKit.Shared.Application;

namespace TourKit.Api.Commission;

/// <summary>
/// Hoa hồng/chia lợi nhuận theo đơn (ProfitSharing hệ cũ) — dưới /api/v1/orders/{orderId}/profit(-shares).
/// Lợi nhuận đơn = doanh thu − chi phí, tính duy nhất tại <see cref="TourKit.Infrastructure.Domain.OrderMath.Profit"/>.
/// Endpoint mỏng: map request → command/query → dispatch → map Result sang HTTP (conventions §6).
/// </summary>
public static class CommissionEndpoints
{
    public static IEndpointRouteBuilder MapCommissionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/orders/{orderId:guid}/profit", async (
            Guid orderId, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new GetOrderProfitQuery(orderId), ct))
                .Match(p => Results.Ok(p))).RequireAuthorization(Permissions.CommissionView);

        app.MapPost("/api/v1/orders/{orderId:guid}/profit-shares", async (
            Guid orderId, CreateProfitShareRequest body, IDispatcher dispatcher, CancellationToken ct) =>
        {
            var command = new CreateProfitShareCommand(orderId, body.UserId, body.Percentage);
            var result = await dispatcher.Send(command, ct);
            return result.Match(s => Results.Created($"/api/v1/orders/{orderId}/profit-shares/{s.Id}", s));
        }).RequireAuthorization(Permissions.CommissionCreate);

        app.MapGet("/api/v1/orders/{orderId:guid}/profit-shares", async (
            Guid orderId, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new ListProfitSharesQuery(orderId), ct))
                .Match(list => Results.Ok(list))).RequireAuthorization(Permissions.CommissionView);

        return app;
    }
}
