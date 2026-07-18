using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Infrastructure.Reports;
using TourKit.Shared.Entities;
using TourKit.Shared.Tenancy;

namespace TourKit.UnitTests.Reports;

/// <summary>
/// Test report Hoa hồng theo mốc (bậc thang) trực tiếp qua handler — mirror <c>CommissionByUserReportTests</c>.
/// Khác report phẳng: % lấy từ bậc lợi nhuận của <see cref="CommissionCampaign"/>, xuất cả hoa hồng theo
/// lợi nhuận (ComByProfit) lẫn theo doanh thu (ComByRevenue).
/// </summary>
public class CommissionByMilestoneReportTests
{
    private sealed class FixedTenant : ITenantContext
    {
        public Guid TenantId { get; init; } = Guid.NewGuid();
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private static AppDbContext NewDb(ITenantContext tenant) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);

    /// <summary>Tạo campaign bậc thang [0,5tr)→5%, [5tr,∞)→10% cho user, đang áp dụng quanh hôm nay.</summary>
    private static void SeedCampaign(AppDbContext db, Guid tenantId, Guid userId)
    {
        var campaign = new CommissionCampaign
        {
            TenantId = tenantId, Name = "CS 2026", Status = 0,
            StartDate = DateTimeOffset.Now.AddDays(-30), EndDate = DateTimeOffset.Now.AddDays(30),
        };
        db.CommissionCampaigns.Add(campaign);
        db.CommissionCampaignUsers.Add(new CommissionCampaignUser
        {
            TenantId = tenantId, CommissionCampaignId = campaign.Id, UserId = userId,
        });
        db.CommissionTiers.Add(new CommissionTier
        {
            TenantId = tenantId, CommissionCampaignId = campaign.Id, StartAmount = 0m, EndAmount = 5_000_000m, Percentage = 5m,
        });
        db.CommissionTiers.Add(new CommissionTier
        {
            TenantId = tenantId, CommissionCampaignId = campaign.Id, StartAmount = 5_000_000m, EndAmount = 999_000_000m, Percentage = 10m,
        });
    }

    [Fact]
    public async Task Ap_bac_thang_theo_loi_nhuan_va_xuat_ca_hoa_hong_theo_doanh_thu()
    {
        var tenant = new FixedTenant();
        var db = NewDb(tenant);
        var salesUserId = Guid.NewGuid();

        var order = new Order { TenantId = tenant.TenantId, Code = "ORD-1", TotalRevenue = 10_000_000m, SalesUserId = salesUserId };
        db.Orders.Add(order);
        db.OrderCosts.Add(new OrderCost
        {
            TenantId = tenant.TenantId, OrderId = order.Id, ProviderId = Guid.NewGuid(), ActualAmount = 3_000_000m,
        });
        SeedCampaign(db, tenant.TenantId, salesUserId);
        await db.SaveChangesAsync();

        var rows = await new ReportQueries(db).GetCommissionByMilestoneAsync(null, null);

        var row = Assert.Single(rows);
        Assert.Equal(salesUserId, row.UserId);
        Assert.Equal(10_000_000m, row.Turnover);
        Assert.Equal(3_000_000m, row.Cost);
        Assert.Equal(7_000_000m, row.Profit);      // profit rơi bậc 2 (>=5tr) → 10%
        Assert.Equal(10m, row.CommissionRate);
        Assert.Equal(700_000m, row.CommissionByProfit);    // 7tr × 10%
        Assert.Equal(1_000_000m, row.CommissionByRevenue); // 10tr × 10%
        Assert.Equal("CS 2026", row.CampaignName);
    }

    [Fact]
    public async Task Khong_co_campaign_thi_rate_0_nhung_van_xuat_dong()
    {
        var tenant = new FixedTenant();
        var db = NewDb(tenant);
        var salesUserId = Guid.NewGuid();

        var order = new Order { TenantId = tenant.TenantId, Code = "ORD-1", TotalRevenue = 8_000_000m, SalesUserId = salesUserId };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var rows = await new ReportQueries(db).GetCommissionByMilestoneAsync(null, null);

        var row = Assert.Single(rows);
        Assert.Equal(8_000_000m, row.Turnover);
        Assert.Equal(8_000_000m, row.Profit);
        Assert.Equal(0m, row.CommissionRate);
        Assert.Equal(0m, row.CommissionByProfit);
        Assert.Equal(0m, row.CommissionByRevenue);
        Assert.Null(row.CampaignName);
    }

    [Fact]
    public async Task Loc_theo_khoang_ngay_tao_don()
    {
        var tenant = new FixedTenant();
        var db = NewDb(tenant);
        var salesUserId = Guid.NewGuid();

        // AppDbContext stamp CreatedAt=now khi Added; đặt ngày quá khứ ở lần save thứ 2 (Modified không đụng CreatedAt).
        var inOrder = new Order { TenantId = tenant.TenantId, Code = "IN", TotalRevenue = 6_000_000m, SalesUserId = salesUserId };
        var outOrder = new Order { TenantId = tenant.TenantId, Code = "OUT", TotalRevenue = 99_000_000m, SalesUserId = salesUserId };
        db.Orders.Add(inOrder);
        db.Orders.Add(outOrder);
        await db.SaveChangesAsync();

        inOrder.CreatedAt = new DateTimeOffset(2026, 3, 15, 0, 0, 0, TimeSpan.Zero);  // trong khoảng
        outOrder.CreatedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);   // trước from → loại
        await db.SaveChangesAsync();

        var from = new DateTimeOffset(2026, 3, 1, 0, 0, 0, TimeSpan.Zero);
        var to = new DateTimeOffset(2026, 3, 31, 0, 0, 0, TimeSpan.Zero);
        var rows = await new ReportQueries(db).GetCommissionByMilestoneAsync(from, to);

        var row = Assert.Single(rows);
        Assert.Equal(6_000_000m, row.Turnover); // chỉ đơn IN
    }
}
