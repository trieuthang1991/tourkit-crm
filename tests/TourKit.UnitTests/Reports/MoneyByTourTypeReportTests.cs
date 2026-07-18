using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Infrastructure.Reports;
using TourKit.Shared.Entities;
using TourKit.Shared.Tenancy;

namespace TourKit.UnitTests.Reports;

/// <summary>
/// Test report Thu–chi theo loại tour (legacy MoneyReport nhánh FIT/GIT + hoàn/huỷ): gom theo BookingType,
/// doanh thu ròng trừ hoàn, lợi nhuận = ròng − chi.
/// </summary>
public class MoneyByTourTypeReportTests
{
    private sealed class FixedTenant : ITenantContext
    {
        public Guid TenantId { get; init; } = Guid.NewGuid();
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private static AppDbContext NewDb(ITenantContext tenant) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);

    [Fact]
    public async Task Gom_theo_loai_tour_va_tru_hoan_huy()
    {
        var tenant = new FixedTenant();
        var db = NewDb(tenant);

        // FIT (BookingType 0): 2 đơn, có hoàn.
        var fit1 = new Order { TenantId = tenant.TenantId, Code = "FIT-1", BookingType = 0, TotalRevenue = 10_000_000m, TotalRefund = 2_000_000m };
        var fit2 = new Order { TenantId = tenant.TenantId, Code = "FIT-2", BookingType = 0, TotalRevenue = 5_000_000m, TotalRefund = 0m };
        // GIT (BookingType 1): 1 đơn.
        var git1 = new Order { TenantId = tenant.TenantId, Code = "GIT-1", BookingType = 1, TotalRevenue = 20_000_000m, TotalRefund = 0m };
        db.Orders.AddRange(fit1, fit2, git1);
        db.OrderCosts.Add(new OrderCost { TenantId = tenant.TenantId, OrderId = fit1.Id, ProviderId = Guid.NewGuid(), ActualAmount = 3_000_000m });
        db.OrderCosts.Add(new OrderCost { TenantId = tenant.TenantId, OrderId = git1.Id, ProviderId = Guid.NewGuid(), ActualAmount = 8_000_000m });
        await db.SaveChangesAsync();

        var rows = await new ReportQueries(db).GetMoneyByTourTypeAsync();

        Assert.Equal(2, rows.Count);
        var fit = Assert.Single(rows, r => r.BookingType == 0);
        Assert.Equal(2, fit.OrderCount);
        Assert.Equal(15_000_000m, fit.GrossRevenue);   // 10tr + 5tr
        Assert.Equal(2_000_000m, fit.Refund);
        Assert.Equal(13_000_000m, fit.NetRevenue);      // gộp − hoàn
        Assert.Equal(3_000_000m, fit.Cost);
        Assert.Equal(10_000_000m, fit.Profit);          // ròng − chi
        Assert.Contains("FIT", fit.TourTypeName);

        var git = Assert.Single(rows, r => r.BookingType == 1);
        Assert.Equal(20_000_000m, git.NetRevenue);
        Assert.Equal(12_000_000m, git.Profit);          // 20tr − 8tr
    }

    [Fact]
    public async Task Khong_co_don_thi_rong()
    {
        var tenant = new FixedTenant();
        var db = NewDb(tenant);
        var rows = await new ReportQueries(db).GetMoneyByTourTypeAsync();
        Assert.Empty(rows);
    }
}
