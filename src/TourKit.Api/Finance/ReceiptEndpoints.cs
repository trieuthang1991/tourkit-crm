using Microsoft.EntityFrameworkCore;
using TourKit.Api.Authz;
using TourKit.Infrastructure.Domain;
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
                Status = 0,           // 0 = chờ duyệt
                IsRecognized = false, // chưa ghi nhận dòng tiền tới khi duyệt (legacy IsGhiNhanDongTien)
            };
            db.ReceiptVouchers.Add(receipt);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/orders/{orderId}/receipts/{receipt.Id}", ToResponse(receipt));
        }).RequireAuthorization(Permissions.ReceiptCreate);

        // Duyệt phiếu → ghi nhận dòng tiền (mới tính vào công nợ). Mode 1 cấp (Default).
        app.MapPost("/api/v1/receipts/{receiptId:guid}/approve", async (
            Guid receiptId, AppDbContext db, CancellationToken ct) =>
        {
            var receipt = await db.ReceiptVouchers.FirstOrDefaultAsync(r => r.Id == receiptId, ct);
            if (receipt is null)
            {
                return Results.NotFound();
            }

            receipt.Status = 1;          // 1 = đã duyệt
            receipt.IsRecognized = true;
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToResponse(receipt));
        }).RequireAuthorization(Permissions.ReceiptApprove);

        // Không duyệt (từ chối) → không ghi nhận.
        app.MapPost("/api/v1/receipts/{receiptId:guid}/reject", async (
            Guid receiptId, AppDbContext db, CancellationToken ct) =>
        {
            var receipt = await db.ReceiptVouchers.FirstOrDefaultAsync(r => r.Id == receiptId, ct);
            if (receipt is null)
            {
                return Results.NotFound();
            }

            receipt.Status = 2;          // 2 = từ chối
            receipt.IsRecognized = false;
            await db.SaveChangesAsync(ct);
            return Results.Ok(ToResponse(receipt));
        }).RequireAuthorization(Permissions.ReceiptApprove);

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

            // Chỉ phiếu ĐÃ DUYỆT mới tính (quy tắc ở ReceiptQueries.Recognized — một chỗ).
            var paid = await db.ReceiptVouchers
                .Where(r => r.OrderId == orderId).Recognized()
                .SumAsync(r => r.Amount, ct);
            return Results.Ok(new OrderBalanceResponse(orderId, order.TotalRevenue, paid, order.TotalRevenue - paid));
        }).RequireAuthorization(Permissions.ReceiptView);

        return app;
    }

    private static ReceiptResponse ToResponse(ReceiptVoucher r) => new(
        r.Id, r.Code, r.OrderId, r.Amount, r.PaymentMethod, r.IssuedAt, r.Partner, r.Note, r.Status, r.IsRecognized);
}
