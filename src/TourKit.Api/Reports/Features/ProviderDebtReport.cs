using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Reports.Features;

/// <summary>Một dòng công nợ nhà cung cấp: tổng chi phí, đã chi (phiếu đã duyệt), còn phải trả.</summary>
public sealed record ProviderDebtRow(
    Guid ProviderId, string ProviderName, decimal TotalCost, decimal Paid, decimal Outstanding);

/// <summary>
/// Báo cáo công nợ phải trả NCC (đối xứng OrderDebtReport — công nợ phải thu): gom theo ProviderId.
/// TotalCost = Σ OrderCost.ActualAmount; Paid = Σ PaymentVoucher.Amount đã ghi nhận (IsRecognized).
/// Tính động bằng truy vấn (grid nhỏ). Khi dữ liệu lớn/sort nặng → Materialized View + đồng bộ ở app (DB §F4/§I).
/// </summary>
public sealed record ProviderDebtReportQuery : IQuery<IReadOnlyList<ProviderDebtRow>>;

public sealed class ProviderDebtReportHandler : IQueryHandler<ProviderDebtReportQuery, IReadOnlyList<ProviderDebtRow>>
{
    private readonly AppDbContext _db;

    public ProviderDebtReportHandler(AppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<ProviderDebtRow>>> Handle(ProviderDebtReportQuery q, CancellationToken ct)
    {
        // 3 truy vấn top-level rồi ghép ở memory — tránh subquery tương quan (không dịch được extension,
        // và tránh ORDER BY decimal trên SQLite). Grid lớn → Materialized View (§F4).
        var costs = await _db.OrderCosts.AsNoTracking()
            .GroupBy(c => c.ProviderId)
            .Select(g => new { ProviderId = g.Key, Total = g.Sum(x => x.ActualAmount) })
            .ToListAsync(ct);

        var paid = await _db.PaymentVouchers.AsNoTracking()
            .Where(p => p.IsRecognized && p.ProviderId != null)
            .GroupBy(p => p.ProviderId!.Value)
            .Select(g => new { ProviderId = g.Key, Paid = g.Sum(x => x.Amount) })
            .ToListAsync(ct);

        var providers = await _db.Providers.AsNoTracking()
            .Select(p => new { p.Id, p.Name })
            .ToListAsync(ct);

        var ids = costs.Select(c => c.ProviderId).Union(paid.Select(p => p.ProviderId)).Distinct();

        IReadOnlyList<ProviderDebtRow> rows = ids
            .Select(id =>
            {
                var total = costs.FirstOrDefault(c => c.ProviderId == id)?.Total ?? 0m;
                var pd = paid.FirstOrDefault(p => p.ProviderId == id)?.Paid ?? 0m;
                var name = providers.FirstOrDefault(p => p.Id == id)?.Name ?? id.ToString();
                return new ProviderDebtRow(id, name, total, pd, total - pd);
            })
            .Where(r => r.TotalCost > 0 || r.Paid > 0)
            .OrderByDescending(r => r.Outstanding)
            .ToList();

        return Result.Success(rows);
    }
}
