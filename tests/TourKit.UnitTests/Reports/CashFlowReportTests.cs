using Microsoft.EntityFrameworkCore;
using TourKit.Api.Reports.Features;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Tenancy;

namespace TourKit.UnitTests.Reports;

/// <summary>
/// Test report Dòng tiền theo phương thức thanh toán trực tiếp qua handler — nhanh, KHÔNG HTTP, KHÔNG server
/// (cùng cách với <c>ProviderDebtReportTests</c>).
/// </summary>
public class CashFlowReportTests
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
    public async Task Report_gom_dung_dong_tien_theo_phuong_thuc_chi_tinh_phieu_da_ghi_nhan()
    {
        var tenant = new FixedTenant();
        var db = NewDb(tenant);

        var order = new Order { TenantId = tenant.TenantId, Code = "ORD-1", TotalRevenue = 5_000_000m };
        db.Orders.Add(order);

        // Phiếu thu ĐÃ ghi nhận — cash 2tr, bank 1tr.
        db.ReceiptVouchers.Add(new ReceiptVoucher
        {
            TenantId = tenant.TenantId, Code = "REC-1", PaymentMethod = "cash", OrderId = order.Id,
            Amount = 2_000_000m, Status = 1, IsRecognized = true,
        });
        db.ReceiptVouchers.Add(new ReceiptVoucher
        {
            TenantId = tenant.TenantId, Code = "REC-2", PaymentMethod = "bank", OrderId = order.Id,
            Amount = 1_000_000m, Status = 1, IsRecognized = true,
        });
        // Phiếu thu CHƯA ghi nhận — không được tính.
        db.ReceiptVouchers.Add(new ReceiptVoucher
        {
            TenantId = tenant.TenantId, Code = "REC-3", PaymentMethod = "cash", OrderId = order.Id,
            Amount = 500_000m, Status = 0, IsRecognized = false,
        });

        // Phiếu chi ĐÃ ghi nhận — cash 1tr.
        db.PaymentVouchers.Add(new PaymentVoucher
        {
            TenantId = tenant.TenantId, Code = "PAY-1", PaymentMethod = "cash", OrderId = order.Id,
            Amount = 1_000_000m, Status = 1, IsRecognized = true,
        });
        // Phiếu chi CHƯA ghi nhận — không được tính.
        db.PaymentVouchers.Add(new PaymentVoucher
        {
            TenantId = tenant.TenantId, Code = "PAY-2", PaymentMethod = "bank", OrderId = order.Id,
            Amount = 300_000m, Status = 0, IsRecognized = false,
        });

        await db.SaveChangesAsync();

        var handler = new CashFlowReportHandler(db);
        var result = await handler.Handle(new CashFlowReportQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);

        var cash = result.Value.Single(r => r.PaymentMethod == "cash");
        Assert.Equal(2_000_000m, cash.Inflow);
        Assert.Equal(1_000_000m, cash.Outflow);
        Assert.Equal(1_000_000m, cash.Net);

        var bank = result.Value.Single(r => r.PaymentMethod == "bank");
        Assert.Equal(1_000_000m, bank.Inflow);
        Assert.Equal(0m, bank.Outflow);
        Assert.Equal(1_000_000m, bank.Net);
    }
}
