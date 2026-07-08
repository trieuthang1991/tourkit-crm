using Microsoft.EntityFrameworkCore;
using TourKit.Api.Authz;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;

namespace TourKit.Api.Finance;

/// <summary>Phiếu thu + công nợ theo đơn. Công nợ = Order.TotalRevenue − tổng phiếu thu (tính động).</summary>
public static class ReceiptEndpoints
{
    public static IEndpointRouteBuilder MapReceiptEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/orders/{orderId:guid}/receipts", async (
            Guid orderId, CreateReceiptRequest body, AppDbContext db, CancellationToken ct) =>
        {
            var orderExists = await db.Orders.AnyAsync(o => o.Id == orderId, ct);
            if (!orderExists)
            {
                return Results.NotFound();
            }

            if (body.Amount <= 0)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["Amount"] = ["Số tiền phải lớn hơn 0."],
                });
            }

            var receipt = new ReceiptVoucher
            {
                Code = "RCP-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
                Title = "Phiếu thu",
                IssuedAt = DateTimeOffset.UtcNow,
                OrderId = orderId,
                Amount = body.Amount,
                PaymentMethod = string.IsNullOrWhiteSpace(body.PaymentMethod) ? "cash" : body.PaymentMethod.Trim(),
                Partner = body.Partner,
                Note = body.Note,
                Status = 0,
                IsRecognized = true,
            };
            db.ReceiptVouchers.Add(receipt);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/orders/{orderId}/receipts/{receipt.Id}", ToResponse(receipt));
        }).RequireAuthorization(Permissions.ReceiptCreate);

        app.MapGet("/api/v1/orders/{orderId:guid}/receipts", async (
            Guid orderId, AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.ReceiptVouchers.AsNoTracking()
                .Where(r => r.OrderId == orderId)
                .OrderBy(r => r.IssuedAt)
                .Select(r => ToResponse(r)).ToListAsync(ct)))
            .RequireAuthorization(Permissions.ReceiptView);

        app.MapGet("/api/v1/orders/{orderId:guid}/balance", async (
            Guid orderId, AppDbContext db, CancellationToken ct) =>
        {
            var order = await db.Orders.AsNoTracking()
                .Where(o => o.Id == orderId)
                .Select(o => new { o.TotalRevenue })
                .FirstOrDefaultAsync(ct);
            if (order is null)
            {
                return Results.NotFound();
            }

            var paid = await db.ReceiptVouchers.Where(r => r.OrderId == orderId).SumAsync(r => r.Amount, ct);
            return Results.Ok(new OrderBalanceResponse(orderId, order.TotalRevenue, paid, order.TotalRevenue - paid));
        }).RequireAuthorization(Permissions.ReceiptView);

        return app;
    }

    private static ReceiptResponse ToResponse(ReceiptVoucher r) => new(
        r.Id, r.Code, r.OrderId, r.Amount, r.PaymentMethod, r.IssuedAt, r.Partner, r.Note);
}
