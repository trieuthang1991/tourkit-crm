using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Entities;
using TourKit.Shared.Security;
using TourKit.Shared.Tenancy;
using TourKit.Tests.Support;

namespace TourKit.Tests.Audit;

public sealed class AuditInterceptorTests
{
    private sealed class TestCurrentUser : ICurrentUserContext
    {
        public Guid? UserId { get; init; }
    }

    private static AppDbContext CreateDb(ITenantContext tenant, ICurrentUserContext user, string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .AddInterceptors(new AuditSaveChangesInterceptor(tenant, user))
            .Options;
        return new AppDbContext(options, tenant);
    }

    [Fact]
    public async Task Insert_writes_activity_log()
    {
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        using var db = CreateDb(
            new TestTenantContext { TenantId = tenantId },
            new TestCurrentUser { UserId = userId },
            nameof(Insert_writes_activity_log));

        db.Customers.Add(new Customer { FullName = "Nguyen Van A" });
        await db.SaveChangesAsync();

        var log = Assert.Single(await db.ActivityLogs.ToListAsync());
        Assert.Equal("Insert", log.Action);
        Assert.Equal("Customer", log.EntityName);
        Assert.Equal(userId, log.UserId);
        Assert.Equal(tenantId, log.TenantId);
    }

    [Fact]
    public async Task Update_writes_activity_log_with_changes()
    {
        using var db = CreateDb(
            new TestTenantContext { TenantId = Guid.NewGuid() },
            new TestCurrentUser { UserId = Guid.NewGuid() },
            nameof(Update_writes_activity_log_with_changes));

        var customer = new Customer { FullName = "A" };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        customer.FullName = "B";
        await db.SaveChangesAsync();

        var updateLog = (await db.ActivityLogs.ToListAsync()).Single(l => l.Action == "Update");
        Assert.Equal("Customer", updateLog.EntityName);
        Assert.NotNull(updateLog.Changes);
        Assert.Contains("FullName", updateLog.Changes);
    }

    [Fact]
    public async Task No_tenant_writes_no_activity_log()
    {
        // Guid.Empty ⇒ HasTenant == false ⇒ interceptor bỏ qua (seed/system op).
        using var db = CreateDb(
            new TestTenantContext { TenantId = Guid.Empty },
            new TestCurrentUser { UserId = null },
            nameof(No_tenant_writes_no_activity_log));

        db.Customers.Add(new Customer { FullName = "System" });
        await db.SaveChangesAsync();

        Assert.Empty(await db.ActivityLogs.IgnoreQueryFilters().ToListAsync());
    }
}
