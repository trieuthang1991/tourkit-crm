using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Entities;
using TourKit.Tests.Support;

namespace TourKit.Tests.Tenancy;

public class TenantReadIsolationTests
{
    [Fact]
    public async Task Can_add_and_read_customer()
    {
        var tenant = new TestTenantContext { TenantId = Guid.NewGuid() };
        var db = TestDb.Create(tenant, nameof(Can_add_and_read_customer));

        db.Customers.Add(new Customer { TenantId = tenant.TenantId, FullName = "Nguyen Van A" });
        await db.SaveChangesAsync();

        var count = await db.Customers.CountAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Tenant_only_sees_its_own_customers()
    {
        var dbName = nameof(Tenant_only_sees_its_own_customers);
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        // Seed: context của tenant A thêm khách của cả A và B (giả lập dữ liệu lẫn lộn ở tầng DB)
        var seedCtx = new TestTenantContext { TenantId = tenantA };
        using (var db = TestDb.Create(seedCtx, dbName))
        {
            db.Customers.Add(new Customer { TenantId = tenantA, FullName = "A-1" });
            db.Customers.Add(new Customer { TenantId = tenantB, FullName = "B-1" });
            await db.SaveChangesAsync();
        }

        // Đọc bằng context của tenant B — chỉ được thấy khách của B
        var ctxB = new TestTenantContext { TenantId = tenantB };
        using (var db = TestDb.Create(ctxB, dbName))
        {
            var names = await db.Customers.Select(c => c.FullName).ToListAsync();
            Assert.Equal(new[] { "B-1" }, names);
        }
    }
}
