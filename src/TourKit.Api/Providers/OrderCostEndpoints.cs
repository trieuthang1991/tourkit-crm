using Microsoft.EntityFrameworkCore;
using TourKit.Api.Authz;
using TourKit.Infrastructure.Domain;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;

namespace TourKit.Api.Providers;

/// <summary>
/// Chi phí trả NCC theo đơn (Order_Chi hệ cũ) — dưới /api/v1/orders/{orderId}/costs.
/// Mỗi lần thêm chi phí, Order.TotalCost được recompute lại từ toàn bộ dòng chi phí (công thức duy nhất
/// ở <see cref="OrderMath.TotalCost"/>) và lưu chung 1 SaveChanges với dòng chi phí mới.
/// </summary>
public static class OrderCostEndpoints
{
    public static IEndpointRouteBuilder MapOrderCostEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/orders/{orderId:guid}/costs", async (
            Guid orderId, CreateOrderCostRequest body, AppDbContext db, CancellationToken ct) =>
        {
            var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == orderId, ct);
            if (order is null)
            {
                return Results.NotFound();
            }

            var providerExists = await db.Providers.AnyAsync(p => p.Id == body.ProviderId, ct);
            if (!providerExists)
            {
                return Invalid("Nhà cung cấp không tồn tại.");
            }

            if (body.ActualAmount < 0m)
            {
                return Invalid("ActualAmount không được âm.");
            }

            var cost = new OrderCost
            {
                OrderId = orderId,
                ProviderId = body.ProviderId,
                ServiceName = body.ServiceName,
                DayIndex = body.DayIndex,
                ExpectedAmount = body.ExpectedAmount,
                ActualAmount = body.ActualAmount,
                Deposit = body.Deposit,
                Surcharge = body.Surcharge,
                Vat = body.Vat,
                Status = body.Status,
            };
            db.OrderCosts.Add(cost);

            // Recompute Order.TotalCost = tổng ActualAmount toàn bộ dòng chi phí của đơn (kể cả dòng mới).
            var existingCosts = await db.OrderCosts.AsNoTracking()
                .Where(c => c.OrderId == orderId).ToListAsync(ct);
            order.TotalCost = OrderMath.TotalCost(existingCosts.Append(cost));

            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/orders/{orderId}/costs/{cost.Id}", ToResponse(cost));
        }).RequireAuthorization(Permissions.CostCreate);

        app.MapGet("/api/v1/orders/{orderId:guid}/costs", async (
            Guid orderId, AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.OrderCosts.AsNoTracking()
                .Where(c => c.OrderId == orderId)
                .OrderBy(c => c.DayIndex)
                .Select(c => ToResponse(c))
                .ToListAsync(ct))).RequireAuthorization(Permissions.CostView);

        return app;
    }

    private static OrderCostResponse ToResponse(OrderCost c) => new(
        c.Id, c.OrderId, c.ProviderId, c.ServiceName, c.DayIndex,
        c.ExpectedAmount, c.ActualAmount, c.Deposit, c.Surcharge, c.Vat, c.Status);

    private static IResult Invalid(string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { ["Request"] = [message] });
}
