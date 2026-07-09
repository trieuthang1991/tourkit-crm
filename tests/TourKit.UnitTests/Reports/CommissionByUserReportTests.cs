using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Infrastructure.Reports;
using TourKit.Shared.Entities;
using TourKit.Shared.Tenancy;

namespace TourKit.UnitTests.Reports;

/// <summary>
/// Test report Hoa hồng/lợi nhuận theo nhân viên trực tiếp qua handler — nhanh, KHÔNG HTTP, KHÔNG server
/// (cùng cách với <c>TurnoverReportTests</c>).
/// </summary>
public class CommissionByUserReportTests
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
    public async Task Report_gom_theo_sales_va_tinh_hoa_hong_tu_rule()
    {
        var tenant = new FixedTenant();
        var db = NewDb(tenant);
        var salesUserId = Guid.NewGuid();

        var order1 = new Order
        {
            TenantId = tenant.TenantId, Code = "ORD-1", TotalRevenue = 5_000_000m, SalesUserId = salesUserId,
        };
        var order2 = new Order
        {
            TenantId = tenant.TenantId, Code = "ORD-2", TotalRevenue = 5_000_000m, SalesUserId = salesUserId,
        };
        db.Orders.Add(order1);
        db.Orders.Add(order2);

        db.OrderCosts.Add(new OrderCost
        {
            TenantId = tenant.TenantId, OrderId = order1.Id, ProviderId = Guid.NewGuid(), ActualAmount = 3_000_000m,
        });

        db.CommissionRules.Add(new CommissionRule
        {
            TenantId = tenant.TenantId, UserId = salesUserId, Percentage = 10m, Status = 0,
        });

        await db.SaveChangesAsync();

        var queries = new ReportQueries(db);
        var rows = await queries.GetCommissionByUserAsync();

        var row = Assert.Single(rows);
        Assert.Equal(salesUserId, row.UserId);
        Assert.Equal(10_000_000m, row.Turnover);
        Assert.Equal(3_000_000m, row.Cost);
        Assert.Equal(7_000_000m, row.Profit);
        Assert.Equal(10m, row.CommissionRate);
        Assert.Equal(700_000m, row.CommissionAmount);
    }

    [Fact]
    public async Task Report_bo_qua_don_khong_co_sales_va_khong_am_hoa_hong_khi_lo()
    {
        var tenant = new FixedTenant();
        var db = NewDb(tenant);
        var salesUserId = Guid.NewGuid();

        // Đơn không có sales — không xuất hiện trong báo cáo.
        db.Orders.Add(new Order { TenantId = tenant.TenantId, Code = "ORD-NOSALES", TotalRevenue = 1_000_000m });

        // Đơn lỗ (cost > revenue): CommissionAmount phải là 0, không âm — kể cả không có rule.
        var lossOrder = new Order
        {
            TenantId = tenant.TenantId, Code = "ORD-LOSS", TotalRevenue = 1_000_000m, SalesUserId = salesUserId,
        };
        db.Orders.Add(lossOrder);
        db.OrderCosts.Add(new OrderCost
        {
            TenantId = tenant.TenantId, OrderId = lossOrder.Id, ProviderId = Guid.NewGuid(), ActualAmount = 2_000_000m,
        });

        await db.SaveChangesAsync();

        var queries = new ReportQueries(db);
        var rows = await queries.GetCommissionByUserAsync();

        var row = Assert.Single(rows);
        Assert.Equal(salesUserId, row.UserId);
        Assert.Equal(-1_000_000m, row.Profit);
        Assert.Equal(0m, row.CommissionRate);
        Assert.Equal(0m, row.CommissionAmount);
    }
}
