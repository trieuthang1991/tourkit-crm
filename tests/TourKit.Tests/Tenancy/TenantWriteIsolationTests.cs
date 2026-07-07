using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Entities;
using TourKit.Tests.Support;

namespace TourKit.Tests.Tenancy;

public class TenantWriteIsolationTests
{
    [Fact]
    public async Task Insert_auto_assigns_current_tenant()
    {
        var tenantId = Guid.NewGuid();
        var ctx = new TestTenantContext { TenantId = tenantId };
        using var db = TestDb.Create(ctx, nameof(Insert_auto_assigns_current_tenant));

        // Cố tình KHÔNG set TenantId
        var customer = new Customer { FullName = "No-Tenant-Set" };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        Assert.Equal(tenantId, customer.TenantId);
        Assert.NotEqual(default, customer.CreatedAt);
    }

    [Fact]
    public async Task Cross_tenant_update_is_blocked()
    {
        var dbName = nameof(Cross_tenant_update_is_blocked);
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        // tenant A tạo 1 khách
        var ctxA = new TestTenantContext { TenantId = tenantA };
        Guid customerId;
        using (var db = TestDb.Create(ctxA, dbName))
        {
            var c = new Customer { FullName = "A-owned" };
            db.Customers.Add(c);
            await db.SaveChangesAsync();
            customerId = c.Id;
        }

        // tenant B nạp thẳng entity của A (bỏ qua filter bằng IgnoreQueryFilters) rồi thử sửa
        var ctxB = new TestTenantContext { TenantId = tenantB };
        using (var db = TestDb.Create(ctxB, dbName))
        {
            var stolen = await db.Customers.IgnoreQueryFilters()
                .SingleAsync(c => c.Id == customerId);
            stolen.FullName = "hacked-by-B";

            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        }
    }
}
