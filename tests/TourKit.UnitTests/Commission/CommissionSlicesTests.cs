using Microsoft.EntityFrameworkCore;
using TourKit.Api.Commission.Features;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;
using TourKit.Shared.Tenancy;

namespace TourKit.UnitTests.Commission;

/// <summary>
/// Test slice Commission trực tiếp qua handler/validator — nhanh, KHÔNG HTTP, KHÔNG server
/// (cùng cách với <c>CustomerSlicesTests</c>).
/// </summary>
public class CommissionSlicesTests
{
    private sealed class FixedTenant : ITenantContext
    {
        public Guid TenantId { get; init; } = Guid.NewGuid();
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private static AppDbContext NewDb(ITenantContext tenant) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);

    private static CreateProfitShareCommand Valid(Guid orderId) => new(orderId, Guid.NewGuid(), 10m);

    [Fact]
    public void Validator_rejects_percentage_0_and_101()
    {
        var v = new CreateProfitShareValidator();

        Assert.False(v.Validate(Valid(Guid.NewGuid()) with { Percentage = 0m }).IsValid);
        Assert.False(v.Validate(Valid(Guid.NewGuid()) with { Percentage = 101m }).IsValid);
        Assert.True(v.Validate(Valid(Guid.NewGuid())).IsValid);
    }

    [Fact]
    public async Task CreateProfitShareHandler_returns_NotFound_for_missing_order()
    {
        var db = NewDb(new FixedTenant());
        var handler = new CreateProfitShareHandler(db);

        var result = await handler.Handle(Valid(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task CreateProfitShareHandler_computes_amount_from_order_profit()
    {
        var db = NewDb(new FixedTenant());
        var order = new Order
        {
            Code = "ORD-COMM",
            TourDepartureId = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            TotalRevenue = 13_000_000m,
            TotalCost = 3_000_000m,
        };
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var handler = new CreateProfitShareHandler(db);
        var result = await handler.Handle(new CreateProfitShareCommand(order.Id, Guid.NewGuid(), 10m), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(10_000_000m, result.Value.ProfitBase);
        Assert.Equal(1_000_000m, result.Value.Amount);
    }
}
