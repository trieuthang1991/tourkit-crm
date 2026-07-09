using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Domain;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Reports.Features;

/// <summary>Tổng quan hoạt động kinh doanh (legacy BusinessActivity/HomePage): doanh thu/thu/chi/công nợ/lợi nhuận.</summary>
public sealed record DashboardSummary(
    int OrderCount,
    decimal TotalRevenue, decimal TotalReceived, decimal ReceivableOutstanding,
    decimal TotalCost, decimal TotalPaid, decimal PayableOutstanding,
    decimal GrossProfit);

/// <summary>
/// Báo cáo Dashboard tổng quan (legacy BusinessActivity/HomePage): một object tổng hợp toàn tenant.
/// Mỗi tổng 1 truy vấn top-level (SUM/COUNT rỗng → 0) — SQLite-safe, không ORDER BY.
/// </summary>
public sealed record DashboardReportQuery : IQuery<DashboardSummary>;

public sealed class DashboardReportHandler : IQueryHandler<DashboardReportQuery, DashboardSummary>
{
    private readonly AppDbContext _db;

    public DashboardReportHandler(AppDbContext db) => _db = db;

    public async Task<Result<DashboardSummary>> Handle(DashboardReportQuery q, CancellationToken ct)
    {
        var orderCount = await _db.Orders.CountAsync(ct);
        var totalRevenue = await _db.Orders.SumAsync(o => o.TotalRevenue, ct);
        var totalReceived = await _db.ReceiptVouchers.Recognized().SumAsync(r => r.Amount, ct);
        var totalCost = await _db.OrderCosts.SumAsync(c => c.ActualAmount, ct);
        var totalPaid = await _db.PaymentVouchers.Where(p => p.IsRecognized).SumAsync(p => p.Amount, ct);

        var summary = new DashboardSummary(
            orderCount,
            totalRevenue, totalReceived, totalRevenue - totalReceived,
            totalCost, totalPaid, totalCost - totalPaid,
            totalRevenue - totalCost);

        return Result.Success(summary);
    }
}
