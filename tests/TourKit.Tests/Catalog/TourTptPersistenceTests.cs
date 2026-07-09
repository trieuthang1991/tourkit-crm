using Microsoft.EntityFrameworkCore;
using TourKit.Shared.Entities;
using TourKit.Tests.Support;

namespace TourKit.Tests.Catalog;

public class TourTptPersistenceTests
{
    [Fact]
    public async Task Template_saved_and_queried_via_OfType()
    {
        var tenant = new TestTenantContext { TenantId = Guid.NewGuid() };
        var db = TestDb.Create(tenant, nameof(Template_saved_and_queried_via_OfType));

        db.TourTemplates.Add(new TourTemplate
        {
            Code = "T-001", Title = "Đà Nẵng 3N2Đ", TotalSlots = 30, PriceAdult = 5_000_000m,
        });
        await db.SaveChangesAsync();

        var templates = await db.Tours.OfType<TourTemplate>().ToListAsync();
        Assert.Single(templates);
        Assert.Equal(TourKind.Template, templates[0].Kind);
        Assert.Equal(5_000_000m, templates[0].PriceAdult);
    }

    [Fact]
    public async Task Templates_are_isolated_per_tenant()
    {
        var dbName = nameof(Templates_are_isolated_per_tenant);
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        using (var db = TestDb.Create(new TestTenantContext { TenantId = tenantA }, dbName))
        {
            db.TourTemplates.Add(new TourTemplate { Code = "A-1", Title = "A" });
            await db.SaveChangesAsync();
        }

        using (var db = TestDb.Create(new TestTenantContext { TenantId = tenantB }, dbName))
        {
            db.TourTemplates.Add(new TourTemplate { Code = "B-1", Title = "B" });
            await db.SaveChangesAsync();

            var titles = await db.TourTemplates.Select(t => t.Title).ToListAsync();
            Assert.Equal(new[] { "B" }, titles);
        }
    }
}
