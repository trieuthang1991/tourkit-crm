using Microsoft.EntityFrameworkCore;
using TourKit.Api.Authz;
using TourKit.Infrastructure.Domain;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;

namespace TourKit.Api.Commission;

/// <summary>
/// Hoa hồng/chia lợi nhuận theo đơn (ProfitSharing hệ cũ) — dưới /api/v1/orders/{orderId}/profit(-shares).
/// Lợi nhuận đơn = doanh thu − chi phí, tính duy nhất tại <see cref="OrderMath.Profit"/>.
/// </summary>
public static class CommissionEndpoints
{
    public static IEndpointRouteBuilder MapCommissionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/orders/{orderId:guid}/profit", async (
            Guid orderId, AppDbContext db, CancellationToken ct) =>
        {
            var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
            if (order is null)
            {
                return Results.NotFound();
            }

            var profit = OrderMath.Profit(order);
            return Results.Ok(new OrderProfitResponse(order.TotalRevenue, order.TotalCost, profit));
        }).RequireAuthorization(Permissions.CommissionView);

        app.MapPost("/api/v1/orders/{orderId:guid}/profit-shares", async (
            Guid orderId, CreateProfitShareRequest body, AppDbContext db, CancellationToken ct) =>
        {
            var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
            if (order is null)
            {
                return Results.NotFound();
            }

            if (body.Percentage <= 0m || body.Percentage > 100m)
            {
                return Invalid("Percentage phải trong khoảng (0, 100].");
            }

            var profit = OrderMath.Profit(order);
            var amount = Math.Round(profit * body.Percentage / 100m, 2);

            var share = new ProfitShare
            {
                OrderId = orderId,
                UserId = body.UserId,
                Percentage = body.Percentage,
                Amount = amount,
                ProfitBase = profit,
            };
            db.ProfitShares.Add(share);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/v1/orders/{orderId}/profit-shares/{share.Id}", ToResponse(share));
        }).RequireAuthorization(Permissions.CommissionCreate);

        app.MapGet("/api/v1/orders/{orderId:guid}/profit-shares", async (
            Guid orderId, AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.ProfitShares.AsNoTracking()
                .Where(s => s.OrderId == orderId)
                .Select(s => ToResponse(s))
                .ToListAsync(ct))).RequireAuthorization(Permissions.CommissionView);

        return app;
    }

    private static ProfitShareResponse ToResponse(ProfitShare s) => new(
        s.Id, s.OrderId, s.UserId, s.Percentage, s.Amount, s.ProfitBase);

    private static IResult Invalid(string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { ["Request"] = [message] });
}
