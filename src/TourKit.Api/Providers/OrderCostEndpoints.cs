using TourKit.Api.Application;
using TourKit.Api.Authz;
using TourKit.Api.Providers.Features;
using TourKit.Shared.Application;

namespace TourKit.Api.Providers;

/// <summary>
/// Chi phí trả NCC theo đơn (Order_Chi hệ cũ) — dưới /api/v1/orders/{orderId}/costs.
/// Mỗi lần thêm chi phí, Order.TotalCost được recompute lại từ toàn bộ dòng chi phí (công thức duy nhất
/// ở <see cref="TourKit.Infrastructure.Domain.OrderMath.TotalCost"/>) và lưu chung 1 SaveChanges với dòng chi phí mới.
/// Endpoint mỏng: map request → command/query → dispatch → map Result sang HTTP (conventions §6).
/// </summary>
public static class OrderCostEndpoints
{
    public static IEndpointRouteBuilder MapOrderCostEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/orders/{orderId:guid}/costs", async (
            Guid orderId, CreateOrderCostRequest body, IDispatcher dispatcher, CancellationToken ct) =>
        {
            var command = new CreateOrderCostCommand(
                orderId, body.ProviderId, body.ServiceName, body.DayIndex, body.ExpectedAmount,
                body.ActualAmount, body.Deposit, body.Surcharge, body.Vat, body.Status);
            var result = await dispatcher.Send(command, ct);
            return result.Match(c => Results.Created($"/api/v1/orders/{orderId}/costs/{c.Id}", c));
        }).RequireAuthorization(Permissions.CostCreate);

        app.MapGet("/api/v1/orders/{orderId:guid}/costs", async (
            Guid orderId, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new ListOrderCostsQuery(orderId), ct))
                .Match(list => Results.Ok(list))).RequireAuthorization(Permissions.CostView);

        return app;
    }
}
