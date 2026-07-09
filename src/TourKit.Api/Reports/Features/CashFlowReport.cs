using Microsoft.EntityFrameworkCore;
using TourKit.Shared.Domain;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Reports.Features;

/// <summary>Một dòng dòng tiền theo phương thức thanh toán: thu vào, chi ra, ròng.</summary>
public sealed record CashFlowRow(string PaymentMethod, decimal Inflow, decimal Outflow, decimal Net);

/// <summary>
/// Báo cáo dòng tiền theo phương thức thanh toán (legacy ListPaymentMethodsCashFlow): gom theo PaymentMethod.
/// Inflow = Σ phiếu thu đã ghi nhận; Outflow = Σ phiếu chi đã ghi nhận. Ghép + sort ở memory (SQLite-safe).
/// </summary>
public sealed record CashFlowReportQuery : IQuery<IReadOnlyList<CashFlowRow>>;

public sealed class CashFlowReportHandler : IQueryHandler<CashFlowReportQuery, IReadOnlyList<CashFlowRow>>
{
    private readonly AppDbContext _db;

    public CashFlowReportHandler(AppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<CashFlowRow>>> Handle(CashFlowReportQuery q, CancellationToken ct)
    {
        // 2 truy vấn top-level (dùng chung ReceiptQueries.Recognized) rồi ghép + sort ở memory —
        // tránh ORDER BY decimal trên SQLite.
        var inflow = await _db.ReceiptVouchers.Recognized()
            .GroupBy(r => r.PaymentMethod)
            .Select(g => new { Method = g.Key, Sum = g.Sum(x => x.Amount) })
            .ToListAsync(ct);
        var outflow = await _db.PaymentVouchers.Where(p => p.IsRecognized)
            .GroupBy(p => p.PaymentMethod)
            .Select(g => new { Method = g.Key, Sum = g.Sum(x => x.Amount) })
            .ToListAsync(ct);

        var methods = inflow.Select(x => x.Method).Union(outflow.Select(x => x.Method)).Distinct();

        IReadOnlyList<CashFlowRow> rows = methods
            .Select(m =>
            {
                var i = inflow.FirstOrDefault(x => x.Method == m)?.Sum ?? 0m;
                var o = outflow.FirstOrDefault(x => x.Method == m)?.Sum ?? 0m;
                return new CashFlowRow(m, i, o, i - o);
            })
            .OrderByDescending(r => r.Net)
            .ToList();

        return Result.Success(rows);
    }
}
