using Microsoft.EntityFrameworkCore;
using TourKit.Api.Billing.Features;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;
using TourKit.Shared.Tenancy;

namespace TourKit.UnitTests.Billing;

/// <summary>
/// Test slice Billing trực tiếp qua handler/validator — nhanh, KHÔNG HTTP, KHÔNG server
/// (cùng cách với <c>CustomerSlicesTests</c>).
/// </summary>
public class BillingSlicesTests
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
    public void Validator_rejects_empty_plan_code()
    {
        var v = new ChangePlanValidator();

        Assert.False(v.Validate(new ChangePlanCommand("")).IsValid);
        Assert.True(v.Validate(new ChangePlanCommand("pro")).IsValid);
    }

    [Fact]
    public async Task ChangePlanHandler_returns_Validation_for_unknown_plan_code()
    {
        var db = NewDb(new FixedTenant());
        var handler = new ChangePlanHandler(db);

        var result = await handler.Handle(new ChangePlanCommand("nonexistent"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
    }
}
