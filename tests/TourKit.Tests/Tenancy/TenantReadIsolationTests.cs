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

        // Seed: mỗi tenant thêm khách qua context của chính mình (interceptor tự gán TenantId).
        // Không thể chèn dữ liệu tenant khác từ một context — đó chính là bảo đảm cô lập ghi.
        using (var db = TestDb.Create(new TestTenantContext { TenantId = tenantA }, dbName))
        {
            db.Customers.Add(new Customer { FullName = "A-1" });
            await db.SaveChangesAsync();
        }

        using (var db = TestDb.Create(new TestTenantContext { TenantId = tenantB }, dbName))
        {
            db.Customers.Add(new Customer { FullName = "B-1" });
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
