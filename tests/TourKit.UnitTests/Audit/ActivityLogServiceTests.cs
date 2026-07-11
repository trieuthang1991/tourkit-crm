using TourKit.Application.Audit;
using TourKit.Shared.Entities;
using TourKit.UnitTests.Booking; // FakeRepository<T> generic dùng chung

namespace TourKit.UnitTests.Audit;

public sealed class ActivityLogServiceTests
{
    private static async Task<ActivityLogService> NewServiceAsync()
    {
        var repo = new FakeRepository<ActivityLog>();
        await repo.AddAsync(new ActivityLog { Action = "Insert", EntityName = "Customer", EntityId = "c1" });
        await repo.AddAsync(new ActivityLog { Action = "Update", EntityName = "Order", EntityId = "o1" });
        await repo.SaveChangesAsync();
        return new ActivityLogService(repo);
    }

    [Fact]
    public async Task ListAsync_returns_all_without_filter()
    {
        var svc = await NewServiceAsync();
        var all = await svc.ListAsync(1, 20, null, null);
        Assert.Equal(2, all.Total);
    }

    [Fact]
    public async Task ListAsync_filters_by_entity_name()
    {
        var svc = await NewServiceAsync();
        var customers = await svc.ListAsync(1, 20, "Customer", null);
        var one = Assert.Single(customers.Items);
        Assert.Equal("Customer", one.EntityName);
    }

    [Fact]
    public async Task ListAsync_filters_by_entity_name_and_id()
    {
        var svc = await NewServiceAsync();
        var byId = await svc.ListAsync(1, 20, "Order", "o1");
        Assert.Single(byId.Items);

        var none = await svc.ListAsync(1, 20, "Order", "nope");
        Assert.Empty(none.Items);
    }
}
