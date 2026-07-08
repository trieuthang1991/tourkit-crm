using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Domain;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Reports.Features;

/// <summary>
/// Báo cáo công nợ phải thu (legacy MoneyReport CNPT): các đơn còn nợ = TotalRevenue − tổng phiếu thu ĐÃ DUYỆT.
/// Tính động bằng truy vấn (grid nhỏ). Khi dữ liệu lớn/sort nặng → Materialized View + đồng bộ ở app (DB §F4/§I).
/// </summary>
public sealed record OrderDebtReportQuery : IQuery<IReadOnlyList<OrderDebtRow>>;

public sealed class OrderDebtReportHandler : IQueryHandler<OrderDebtReportQuery, IReadOnlyList<OrderDebtRow>>
{
    private readonly AppDbContext _db;

    public OrderDebtReportHandler(AppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<OrderDebtRow>>> Handle(OrderDebtReportQuery q, CancellationToken ct)
    {
        // 2 truy vấn top-level (dùng chung ReceiptQueries.Recognized) rồi ghép ở memory —
        // tránh subquery tương quan (không dịch được extension). Grid lớn → Materialized View (§F4).
        var orders = await _db.Orders.AsNoTracking()
            .Select(o => new { o.Id, o.Code, o.CustomerId, o.TotalRevenue })
            .ToListAsync(ct);

        var paidByOrder = (await _db.ReceiptVouchers.Recognized()
                .GroupBy(r => r.OrderId)
                .Select(g => new { OrderId = g.Key, Paid = g.Sum(r => r.Amount) })
                .ToListAsync(ct))
            .ToDictionary(x => x.OrderId, x => x.Paid);

        IReadOnlyList<OrderDebtRow> rows = orders
            .Select(o => (o, Paid: paidByOrder.GetValueOrDefault(o.Id, 0m)))
            .Where(x => x.o.TotalRevenue - x.Paid > 0m)
            .OrderByDescending(x => x.o.TotalRevenue - x.Paid)
            .Select(x => new OrderDebtRow(
                x.o.Id, x.o.Code, x.o.CustomerId, x.o.TotalRevenue, x.Paid, x.o.TotalRevenue - x.Paid))
            .ToList();

        return Result.Success(rows);
    }
}
