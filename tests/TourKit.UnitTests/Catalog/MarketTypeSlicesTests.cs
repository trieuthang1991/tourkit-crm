using Microsoft.EntityFrameworkCore;
using TourKit.Api.Catalog.Features;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Tenancy;

namespace TourKit.UnitTests.Catalog;

public sealed class MarketTypeSlicesTests
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
    public void Create_validator_rejects_empty_name()
    {
        var r = new CreateMarketTypeValidator().Validate(new CreateMarketTypeCommand("", null, 0));
        Assert.False(r.IsValid);
    }

    [Fact]
    public async Task Update_then_delete_roundtrip()
    {
        var db = NewDb(new FixedTenant());
        var create = await new CreateMarketTypeHandler(db).Handle(new CreateMarketTypeCommand("Inbound", null, 1), default);
        Assert.True(create.IsSuccess);
        var id = create.Value.Id;

        var upd = await new UpdateMarketTypeHandler(db).Handle(new UpdateMarketTypeCommand(id, "Outbound", null, 2), default);
        Assert.True(upd.IsSuccess);

        var del = await new DeleteMarketTypeHandler(db).Handle(new DeleteMarketTypeCommand(id), default);
        Assert.True(del.IsSuccess);
    }
}
