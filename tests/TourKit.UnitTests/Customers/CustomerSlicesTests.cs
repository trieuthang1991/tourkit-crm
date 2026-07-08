using Microsoft.EntityFrameworkCore;
using TourKit.Api.Customers.Features;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;
using TourKit.Shared.Tenancy;

namespace TourKit.UnitTests.Customers;

/// <summary>
/// Test slice Customer trực tiếp qua handler/validator — nhanh, KHÔNG HTTP, KHÔNG server
/// (cùng cách với <c>CreateTourTemplateTests</c>).
/// </summary>
public class CustomerSlicesTests
{
    private sealed class FixedTenant : ITenantContext
    {
        public Guid TenantId { get; init; } = Guid.NewGuid();
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private static AppDbContext NewDb(ITenantContext tenant) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);

    private static CreateCustomerCommand Valid() => new("Nguyen Van A", "0900000000");

    [Fact]
    public void Validator_rejects_empty_full_name()
    {
        var v = new CreateCustomerValidator();

        Assert.False(v.Validate(Valid() with { FullName = "" }).IsValid);
        Assert.True(v.Validate(Valid()).IsValid);
    }

    [Fact]
    public async Task CreateCustomerHandler_creates_and_returns_response()
    {
        var db = NewDb(new FixedTenant());
        var handler = new CreateCustomerHandler(db);

        var result = await handler.Handle(Valid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Nguyen Van A", result.Value.FullName);
        Assert.Equal(1, await db.Customers.CountAsync());
    }

    [Fact]
    public async Task DeleteCustomerHandler_returns_NotFound_for_missing_id()
    {
        var db = NewDb(new FixedTenant());
        var handler = new DeleteCustomerHandler(db);

        var result = await handler.Handle(new DeleteCustomerCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }
}
