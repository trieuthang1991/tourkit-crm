using Microsoft.EntityFrameworkCore;
using TourKit.Api.Reports.Features;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Tenancy;

namespace TourKit.UnitTests.Reports;

/// <summary>
/// Test report Doanh thu–lợi nhuận theo đơn trực tiếp qua handler — nhanh, KHÔNG HTTP, KHÔNG server
/// (cùng cách với <c>OrderDebtReportTests</c>/<c>ProviderDebtReportTests</c>).
/// </summary>
public class TurnoverReportTests
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
    public async Task Report_tinh_dung_doanh_thu_chi_phi_loi_nhuan_theo_don()
    {
        var tenant = new FixedTenant();
        var db = NewDb(tenant);

        var order = new Order { TenantId = tenant.TenantId, Code = "ORD-1", TotalRevenue = 5_000_000m };
        db.Orders.Add(order);

        db.OrderCosts.Add(new OrderCost
        {
            TenantId = tenant.TenantId, OrderId = order.Id, ProviderId = Guid.NewGuid(), ActualAmount = 2_000_000m,
        });
        db.OrderCosts.Add(new OrderCost
        {
            TenantId = tenant.TenantId, OrderId = order.Id, ProviderId = Guid.NewGuid(), ActualAmount = 1_000_000m,
        });

        await db.SaveChangesAsync();

        var handler = new TurnoverReportHandler(db);
        var result = await handler.Handle(new TurnoverReportQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var row = Assert.Single(result.Value);
        Assert.Equal(order.Id, row.OrderId);
        Assert.Equal("ORD-1", row.OrderCode);
        Assert.Equal(5_000_000m, row.Revenue);
        Assert.Equal(3_000_000m, row.Cost);
        Assert.Equal(2_000_000m, row.Profit);
    }
}
