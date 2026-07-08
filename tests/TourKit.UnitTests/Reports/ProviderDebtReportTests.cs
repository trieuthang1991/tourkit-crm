using Microsoft.EntityFrameworkCore;
using TourKit.Api.Reports.Features;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Tenancy;

namespace TourKit.UnitTests.Reports;

/// <summary>
/// Test report Provider Debt trực tiếp qua handler — nhanh, KHÔNG HTTP, KHÔNG server
/// (cùng cách với <c>PaymentSlicesTests</c>).
/// </summary>
public class ProviderDebtReportTests
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
    public async Task Report_tinh_dung_cong_no_theo_provider_chi_tru_phieu_da_ghi_nhan()
    {
        var tenant = new FixedTenant();
        var db = NewDb(tenant);

        var provider = new Provider { TenantId = tenant.TenantId, Code = "NCC-1", Name = "Khách sạn A" };
        db.Providers.Add(provider);

        var order = new Order { TenantId = tenant.TenantId, Code = "ORD-1", TotalRevenue = 10_000_000m };
        db.Orders.Add(order);

        db.OrderCosts.Add(new OrderCost
        {
            TenantId = tenant.TenantId, OrderId = order.Id, ProviderId = provider.Id, ActualAmount = 3_000_000m,
        });
        db.OrderCosts.Add(new OrderCost
        {
            TenantId = tenant.TenantId, OrderId = order.Id, ProviderId = provider.Id, ActualAmount = 2_000_000m,
        });

        // Phiếu chi ĐÃ ghi nhận — trừ vào công nợ.
        db.PaymentVouchers.Add(new PaymentVoucher
        {
            TenantId = tenant.TenantId, Code = "PAY-1", PaymentMethod = "cash", OrderId = order.Id,
            ProviderId = provider.Id, Amount = 2_000_000m, Status = 1, IsRecognized = true,
        });

        // Phiếu chi CHƯA ghi nhận — không được trừ.
        db.PaymentVouchers.Add(new PaymentVoucher
        {
            TenantId = tenant.TenantId, Code = "PAY-2", PaymentMethod = "cash", OrderId = order.Id,
            ProviderId = provider.Id, Amount = 1_000_000m, Status = 0, IsRecognized = false,
        });

        await db.SaveChangesAsync();

        var handler = new ProviderDebtReportHandler(db);
        var result = await handler.Handle(new ProviderDebtReportQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var row = Assert.Single(result.Value);
        Assert.Equal(provider.Id, row.ProviderId);
        Assert.Equal(5_000_000m, row.TotalCost);
        Assert.Equal(2_000_000m, row.Paid);
        Assert.Equal(3_000_000m, row.Outstanding);
    }
}
