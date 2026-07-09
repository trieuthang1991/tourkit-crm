using Microsoft.EntityFrameworkCore;
using TourKit.Api.Reports.Features;
using TourKit.Shared.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Tenancy;

namespace TourKit.UnitTests.Reports;

/// <summary>
/// Test report Dashboard tổng quan trực tiếp qua handler — nhanh, KHÔNG HTTP, KHÔNG server
/// (cùng cách với <c>ProviderDebtReportTests</c>).
/// </summary>
public class DashboardReportTests
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
    public async Task Dashboard_tinh_dung_doanh_thu_thu_chi_cong_no_loi_nhuan()
    {
        var tenant = new FixedTenant();
        var db = NewDb(tenant);

        var order = new Order { TenantId = tenant.TenantId, Code = "ORD-1", TotalRevenue = 5_000_000m };
        db.Orders.Add(order);

        db.OrderCosts.Add(new OrderCost
        {
            TenantId = tenant.TenantId, OrderId = order.Id, ProviderId = Guid.NewGuid(), ActualAmount = 3_000_000m,
        });

        // Phiếu thu ĐÃ ghi nhận — được tính.
        db.ReceiptVouchers.Add(new ReceiptVoucher
        {
            TenantId = tenant.TenantId, Code = "REC-1", PaymentMethod = "cash", OrderId = order.Id,
            Amount = 2_000_000m, Status = 1, IsRecognized = true,
        });
        // Phiếu thu CHƯA ghi nhận — không được tính.
        db.ReceiptVouchers.Add(new ReceiptVoucher
        {
            TenantId = tenant.TenantId, Code = "REC-2", PaymentMethod = "cash", OrderId = order.Id,
            Amount = 500_000m, Status = 0, IsRecognized = false,
        });

        // Phiếu chi ĐÃ ghi nhận — được tính.
        db.PaymentVouchers.Add(new PaymentVoucher
        {
            TenantId = tenant.TenantId, Code = "PAY-1", PaymentMethod = "cash", OrderId = order.Id,
            Amount = 1_000_000m, Status = 1, IsRecognized = true,
        });
        // Phiếu chi CHƯA ghi nhận — không được tính.
        db.PaymentVouchers.Add(new PaymentVoucher
        {
            TenantId = tenant.TenantId, Code = "PAY-2", PaymentMethod = "cash", OrderId = order.Id,
            Amount = 300_000m, Status = 0, IsRecognized = false,
        });

        await db.SaveChangesAsync();

        var handler = new DashboardReportHandler(db);
        var result = await handler.Handle(new DashboardReportQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var s = result.Value;
        Assert.Equal(1, s.OrderCount);
        Assert.Equal(5_000_000m, s.TotalRevenue);
        Assert.Equal(2_000_000m, s.TotalReceived);
        Assert.Equal(3_000_000m, s.ReceivableOutstanding);
        Assert.Equal(3_000_000m, s.TotalCost);
        Assert.Equal(1_000_000m, s.TotalPaid);
        Assert.Equal(2_000_000m, s.PayableOutstanding);
        Assert.Equal(2_000_000m, s.GrossProfit);
    }
}
