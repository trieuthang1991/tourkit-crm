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
}
