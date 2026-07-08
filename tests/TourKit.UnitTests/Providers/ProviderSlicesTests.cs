using Microsoft.EntityFrameworkCore;
using TourKit.Api.Providers.Features;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;
using TourKit.Shared.Tenancy;

namespace TourKit.UnitTests.Providers;

/// <summary>
/// Test slice Provider/OrderCost trực tiếp qua handler/validator — nhanh, KHÔNG HTTP, KHÔNG server
/// (cùng cách với <c>CustomerSlicesTests</c>/<c>CreateTourTemplateTests</c>).
/// </summary>
public class ProviderSlicesTests
{
    private sealed class FixedTenant : ITenantContext
    {
        public Guid TenantId { get; init; } = Guid.NewGuid();
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private static AppDbContext NewDb(ITenantContext tenant) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);

    private static CreateProviderCommand Valid() => new(
        "NCC-1", "Khách sạn ABC", ProviderType.Hotel, null, null, null,
        null, null, null, null, 0, 1);

    [Fact]
    public void Validator_rejects_empty_code_or_name()
    {
        var v = new CreateProviderValidator();

        Assert.False(v.Validate(Valid() with { Code = "" }).IsValid);
        Assert.False(v.Validate(Valid() with { Name = "" }).IsValid);
        Assert.True(v.Validate(Valid()).IsValid);
    }

    [Fact]
    public async Task CreateProviderHandler_creates_and_returns_response()
    {
        var db = NewDb(new FixedTenant());
        var handler = new CreateProviderHandler(db);

        var result = await handler.Handle(Valid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("NCC-1", result.Value.Code);
        Assert.Equal(1, await db.Providers.CountAsync());
    }

    [Fact]
    public async Task CreateProviderHandler_returns_Conflict_on_duplicate_code()
    {
        var tenant = new FixedTenant();
        var db = NewDb(tenant);
        db.Providers.Add(new Provider { Code = "NCC-1", Name = "Cũ", TenantId = tenant.TenantId });
        await db.SaveChangesAsync();

        var result = await new CreateProviderHandler(db).Handle(Valid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
    }

    [Fact]
    public async Task CreateOrderCostHandler_returns_NotFound_for_missing_order()
    {
        var db = NewDb(new FixedTenant());
        var handler = new CreateOrderCostHandler(db);
        var command = new CreateOrderCostCommand(
            Guid.NewGuid(), Guid.NewGuid(), "X", 1, 100_000m, 100_000m, 0m, 0m, 0m, 1);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }
}
