using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Infrastructure.Reports;
using TourKit.Shared.Entities;
using TourKit.Shared.Tenancy;

namespace TourKit.UnitTests.Reports;

/// <summary>Test 2 báo cáo dashboard mới: hiệu suất theo chi nhánh + top khách hàng (trực tiếp qua ReportQueries).</summary>
public class BranchAndTopCustomerReportTests
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
    public async Task TurnoverByBranch_gom_theo_chi_nhanh_tinh_thuc_thu_con_thieu()
    {
        var tenant = new FixedTenant();
        var db = NewDb(tenant);

        var brA = new Branch { TenantId = tenant.TenantId, Name = "CN A" };
        var brB = new Branch { TenantId = tenant.TenantId, Name = "CN B" };
        db.Branches.AddRange(brA, brB);

        var oA = new Order { TenantId = tenant.TenantId, Code = "OA", BranchId = brA.Id, TotalRevenue = 10_000_000m };
        var oB = new Order { TenantId = tenant.TenantId, Code = "OB", BranchId = brB.Id, TotalRevenue = 4_000_000m };
        db.Orders.AddRange(oA, oB);

        db.OrderCosts.Add(new OrderCost { TenantId = tenant.TenantId, OrderId = oA.Id, ProviderId = Guid.NewGuid(), ActualAmount = 6_000_000m });
        // CN A đã thu 3tr (ghi nhận) → còn thiếu 7tr; CN B chưa thu.
        db.ReceiptVouchers.Add(new ReceiptVoucher { TenantId = tenant.TenantId, Code = "R1", PaymentMethod = "cash", OrderId = oA.Id, Amount = 3_000_000m, Status = 1, IsRecognized = true });
        await db.SaveChangesAsync();

        var rows = await new ReportQueries(db).GetTurnoverByBranchAsync();

        var a = Assert.Single(rows, r => r.BranchName == "CN A");
        Assert.Equal(1, a.OrderCount);
        Assert.Equal(10_000_000m, a.Turnover);
        Assert.Equal(3_000_000m, a.Received);
        Assert.Equal(7_000_000m, a.Outstanding);
        Assert.Equal(4_000_000m, a.Profit); // 10tr - 6tr cost
        var b = Assert.Single(rows, r => r.BranchName == "CN B");
        Assert.Equal(4_000_000m, b.Outstanding); // chưa thu
        // Sắp theo doanh thu giảm dần → CN A đứng trước.
        Assert.Equal("CN A", rows[0].BranchName);
    }

    [Fact]
    public async Task TopCustomers_gom_theo_khach_sap_theo_doanh_thu()
    {
        var tenant = new FixedTenant();
        var db = NewDb(tenant);

        var c1 = new Customer { TenantId = tenant.TenantId, FullName = "Khách Lớn" };
        var c2 = new Customer { TenantId = tenant.TenantId, FullName = "Khách Nhỏ" };
        db.Customers.AddRange(c1, c2);

        var o1 = new Order { TenantId = tenant.TenantId, Code = "O1", CustomerId = c1.Id, TotalRevenue = 20_000_000m };
        var o2 = new Order { TenantId = tenant.TenantId, Code = "O2", CustomerId = c2.Id, TotalRevenue = 5_000_000m };
        db.Orders.AddRange(o1, o2);
        db.ReceiptVouchers.Add(new ReceiptVoucher { TenantId = tenant.TenantId, Code = "R1", PaymentMethod = "cash", OrderId = o1.Id, Amount = 8_000_000m, Status = 1, IsRecognized = true });
        await db.SaveChangesAsync();

        var rows = await new ReportQueries(db).GetTopCustomersAsync();

        Assert.Equal(2, rows.Count);
        Assert.Equal("Khách Lớn", rows[0].CustomerName); // doanh thu cao hơn → đứng đầu
        Assert.Equal(20_000_000m, rows[0].Revenue);
        Assert.Equal(8_000_000m, rows[0].Received);
    }
}
