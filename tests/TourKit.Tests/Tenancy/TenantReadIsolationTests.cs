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
}
