using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Reports.Features;

/// <summary>Một dòng hoa hồng/lợi nhuận theo nhân viên sales.</summary>
public sealed record CommissionByUserRow(
    Guid UserId, decimal Turnover, decimal Cost, decimal Profit, decimal CommissionRate, decimal CommissionAmount);

/// <summary>
/// Báo cáo hoa hồng/lợi nhuận theo nhân viên (legacy ReportTurnoverProfit theo user): gom đơn có
/// <c>SalesUserId</c> theo user, cost từ OrderCost, rate từ <see cref="TourKit.Shared.Entities.CommissionRule"/>
/// (rule đầu tiên của user, mặc định 0 nếu không có). Ghép + sort ở memory (SQLite-safe).
/// </summary>
public sealed record CommissionByUserReportQuery : IQuery<IReadOnlyList<CommissionByUserRow>>;

public sealed class CommissionByUserReportHandler
    : IQueryHandler<CommissionByUserReportQuery, IReadOnlyList<CommissionByUserRow>>
{
    private readonly AppDbContext _db;

    public CommissionByUserReportHandler(AppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<CommissionByUserRow>>> Handle(
        CommissionByUserReportQuery q, CancellationToken ct)
    {
        var orders = await _db.Orders.AsNoTracking()
            .Where(o => o.SalesUserId != null)
            .Select(o => new { o.Id, o.TotalRevenue, UserId = o.SalesUserId!.Value })
            .ToListAsync(ct);

        var costByOrder = (await _db.OrderCosts.AsNoTracking()
                .GroupBy(c => c.OrderId)
                .Select(g => new { OrderId = g.Key, Cost = g.Sum(x => x.ActualAmount) })
                .ToListAsync(ct))
            .ToDictionary(x => x.OrderId, x => x.Cost);

        var rules = (await _db.CommissionRules.AsNoTracking().ToListAsync(ct))
            .GroupBy(r => r.UserId)
            .ToDictionary(g => g.Key, g => g.First().Percentage);

        IReadOnlyList<CommissionByUserRow> rows = orders
            .GroupBy(o => o.UserId)
            .Select(g =>
            {
                var turnover = g.Sum(x => x.TotalRevenue);
                var cost = g.Sum(x => costByOrder.GetValueOrDefault(x.Id, 0m));
                var profit = turnover - cost;
                var rate = rules.GetValueOrDefault(g.Key, 0m);
                var amount = profit > 0m ? profit * rate / 100m : 0m;
                return new CommissionByUserRow(g.Key, turnover, cost, profit, rate, amount);
            })
            .OrderByDescending(r => r.Profit)
            .ToList();

        return Result.Success(rows);
    }
}
