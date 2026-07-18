using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Infrastructure.Reports;
using TourKit.Shared.Entities;
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

        var queries = new ReportQueries(db);
        var rows = await queries.GetProviderDebtAsync();

        var row = Assert.Single(rows);
        Assert.Equal(provider.Id, row.ProviderId);
        Assert.Equal(5_000_000m, row.TotalCost);
        Assert.Equal(2_000_000m, row.Paid);
        Assert.Equal(3_000_000m, row.Outstanding);
    }

    [Fact]
    public async Task Aging_phan_bo_FIFO_tien_da_tra_vao_dong_cu_nhat_truoc()
    {
        var tenant = new FixedTenant();
        var db = NewDb(tenant);

        var provider = new Provider { TenantId = tenant.TenantId, Code = "NCC-1", Name = "Khách sạn A" };
        db.Providers.Add(provider);
        var order = new Order { TenantId = tenant.TenantId, Code = "ORD-1" };
        db.Orders.Add(order);

        var oldCost = new OrderCost { TenantId = tenant.TenantId, OrderId = order.Id, ProviderId = provider.Id, ActualAmount = 3_000_000m };
        var newCost = new OrderCost { TenantId = tenant.TenantId, OrderId = order.Id, ProviderId = provider.Id, ActualAmount = 2_000_000m };
        db.OrderCosts.Add(oldCost);
        db.OrderCosts.Add(newCost);

        // Trả 2tr (đã ghi nhận) — FIFO trừ vào dòng cũ nhất (oldCost) trước.
        db.PaymentVouchers.Add(new PaymentVoucher
        {
            TenantId = tenant.TenantId, Code = "PAY-1", PaymentMethod = "cash", OrderId = order.Id,
            ProviderId = provider.Id, Amount = 2_000_000m, Status = 1, IsRecognized = true,
        });
        await db.SaveChangesAsync();

        // Back-date CreatedAt sau khi lưu (SaveChanges chỉ set CreatedAt lúc Added).
        oldCost.CreatedAt = DateTimeOffset.UtcNow.AddDays(-100);
        newCost.CreatedAt = DateTimeOffset.UtcNow.AddDays(-10);
        await db.SaveChangesAsync();

        var row = Assert.Single(await new ReportQueries(db).GetProviderDebtAsync());
        Assert.Equal(3_000_000m, row.Outstanding);
        Assert.Equal(2_000_000m, row.Current);   // newCost 10 ngày, chưa trả
        Assert.Equal(0m, row.D30);
        Assert.Equal(0m, row.D60);
        Assert.Equal(1_000_000m, row.D90Plus);   // oldCost 3tr − 2tr đã trả = 1tr, 100 ngày
    }

    [Fact]
    public async Task Transactions_tra_ve_so_cai_chi_phi_va_phieu_chi_da_ghi_nhan()
    {
        var tenant = new FixedTenant();
        var db = NewDb(tenant);

        var provider = new Provider { TenantId = tenant.TenantId, Code = "NCC-1", Name = "Khách sạn A" };
        db.Providers.Add(provider);
        var order = new Order { TenantId = tenant.TenantId, Code = "ORD-9" };
        db.Orders.Add(order);
        db.OrderCosts.Add(new OrderCost { TenantId = tenant.TenantId, OrderId = order.Id, ProviderId = provider.Id, ServiceName = "Khách sạn 3 đêm", ActualAmount = 5_000_000m });
        db.PaymentVouchers.Add(new PaymentVoucher
        {
            TenantId = tenant.TenantId, Code = "PAY-9", PaymentMethod = "cash", OrderId = order.Id,
            ProviderId = provider.Id, Amount = 2_000_000m, Status = 1, IsRecognized = true,
        });
        // Phiếu chưa ghi nhận — KHÔNG xuất hiện trong sổ cái.
        db.PaymentVouchers.Add(new PaymentVoucher
        {
            TenantId = tenant.TenantId, Code = "PAY-X", PaymentMethod = "cash", OrderId = order.Id,
            ProviderId = provider.Id, Amount = 1_000_000m, Status = 0, IsRecognized = false,
        });
        await db.SaveChangesAsync();

        var history = await new ReportQueries(db).GetProviderTransactionsAsync(provider.Id);

        Assert.Equal(provider.Id, history.ProviderId);
        Assert.Equal(5_000_000m, history.Summary.TotalCost);
        Assert.Equal(2_000_000m, history.Summary.TotalPaid);
        Assert.Equal(3_000_000m, history.Summary.Remaining);
        Assert.Equal(2, history.Transactions.Count);   // 1 chi phí + 1 phiếu chi đã ghi nhận
        Assert.Contains(history.Transactions, t => t.Type == "cost" && t.RefCode == "ORD-9" && t.Debit == 5_000_000m);
        Assert.Contains(history.Transactions, t => t.Type == "payment" && t.RefCode == "PAY-9" && t.Credit == 2_000_000m);
    }
}
