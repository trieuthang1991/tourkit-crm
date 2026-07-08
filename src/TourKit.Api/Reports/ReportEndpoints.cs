using Microsoft.EntityFrameworkCore;
using TourKit.Api.Authz;
using TourKit.Infrastructure.Domain;
using TourKit.Infrastructure.Persistence;

namespace TourKit.Api.Reports;

/// <summary>
/// Báo cáo công nợ phải thu (legacy MoneyReport CNPT): các đơn còn nợ = TotalRevenue − tổng phiếu thu ĐÃ DUYỆT.
/// Tính động bằng truy vấn (grid nhỏ). Khi dữ liệu lớn/sort nặng → Materialized View + đồng bộ ở app (DB §F4/§I).
/// </summary>
public static class ReportEndpoints
{
    public static IEndpointRouteBuilder MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/reports/order-debt", async (AppDbContext db, CancellationToken ct) =>
        {
            // 2 truy vấn top-level (dùng chung ReceiptQueries.Recognized) rồi ghép ở memory —
            // tránh subquery tương quan (không dịch được extension). Grid lớn → Materialized View (§F4).
            var orders = await db.Orders.AsNoTracking()
                .Select(o => new { o.Id, o.Code, o.CustomerId, o.TotalRevenue })
                .ToListAsync(ct);

            var paidByOrder = (await db.ReceiptVouchers.Recognized()
                    .GroupBy(r => r.OrderId)
                    .Select(g => new { OrderId = g.Key, Paid = g.Sum(r => r.Amount) })
                    .ToListAsync(ct))
                .ToDictionary(x => x.OrderId, x => x.Paid);

            var rows = orders
                .Select(o => (o, Paid: paidByOrder.GetValueOrDefault(o.Id, 0m)))
                .Where(x => x.o.TotalRevenue - x.Paid > 0m)
                .OrderByDescending(x => x.o.TotalRevenue - x.Paid)
                .Select(x => new OrderDebtRow(
                    x.o.Id, x.o.Code, x.o.CustomerId, x.o.TotalRevenue, x.Paid, x.o.TotalRevenue - x.Paid))
                .ToList();
            return Results.Ok(rows);
        }).RequireAuthorization(Permissions.ReportDebtView);

        return app;
    }
}
