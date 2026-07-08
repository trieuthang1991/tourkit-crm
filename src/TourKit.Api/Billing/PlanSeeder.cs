using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;

namespace TourKit.Api.Billing;

/// <summary>Upsert catalog gói vào bảng global Plan (idempotent). Chạy khi startup.</summary>
public static class PlanSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        var existing = await db.Plans.Select(p => p.Code).ToListAsync(ct);
        var missing = PlanCatalog.All.Where(c => !existing.Contains(c.Code)).ToList();
        if (missing.Count == 0)
        {
            return;
        }

        foreach (var (code, name, maxUsers, maxTours, priceMonthly) in missing)
        {
            db.Plans.Add(new Plan
            {
                Code = code,
                Name = name,
                MaxUsers = maxUsers,
                MaxTours = maxTours,
                PriceMonthly = priceMonthly,
            });
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Instance khác seed đồng thời (unique Code) → bỏ qua; lần khởi động sau sẽ nhất quán.
        }
    }
}
