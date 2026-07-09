using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Reports.Features;

/// <summary>Một dòng doanh thu–lợi nhuận theo đơn: doanh thu, chi phí (từ OrderCost), lợi nhuận.</summary>
public sealed record TurnoverRow(Guid OrderId, string OrderCode, decimal Revenue, decimal Cost, decimal Profit);

/// <summary>
/// Báo cáo doanh thu–lợi nhuận theo đơn (legacy ReportTurnover/TurnoverProfit): Cost tính từ OrderCost
/// (không tin cột denormalized), mirror OrderDebtReport. Ghép + sort ở memory (SQLite-safe).
/// </summary>
public sealed record TurnoverReportQuery : IQuery<IReadOnlyList<TurnoverRow>>;

public sealed class TurnoverReportHandler : IQueryHandler<TurnoverReportQuery, IReadOnlyList<TurnoverRow>>
{
    private readonly AppDbContext _db;

    public TurnoverReportHandler(AppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<TurnoverRow>>> Handle(TurnoverReportQuery q, CancellationToken ct)
    {
        // 2 truy vấn top-level rồi ghép ở memory — tránh subquery tương quan (không dịch được extension),
        // và tránh ORDER BY decimal trên SQLite. Grid lớn → Materialized View (§F4).
        var orders = await _db.Orders.AsNoTracking()
            .Select(o => new { o.Id, o.Code, o.TotalRevenue })
            .ToListAsync(ct);

        var costByOrder = (await _db.OrderCosts
                .GroupBy(c => c.OrderId)
                .Select(g => new { OrderId = g.Key, Cost = g.Sum(x => x.ActualAmount) })
                .ToListAsync(ct))
            .ToDictionary(x => x.OrderId, x => x.Cost);

        IReadOnlyList<TurnoverRow> rows = orders
            .Select(o =>
            {
                var cost = costByOrder.GetValueOrDefault(o.Id, 0m);
                return new TurnoverRow(o.Id, o.Code, o.TotalRevenue, cost, o.TotalRevenue - cost);
            })
            .OrderByDescending(r => r.Revenue)
            .ToList();

        return Result.Success(rows);
    }
}
