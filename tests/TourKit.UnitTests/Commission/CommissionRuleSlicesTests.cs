using Microsoft.EntityFrameworkCore;
using TourKit.Api.Commission.Features;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;
using TourKit.Shared.Tenancy;

namespace TourKit.UnitTests.Commission;

/// <summary>
/// Test slice CommissionRule trực tiếp qua handler/validator — nhanh, KHÔNG HTTP, KHÔNG server
/// (cùng cách với <c>CustomerSlicesTests</c>).
/// </summary>
public class CommissionRuleSlicesTests
{
    private sealed class FixedTenant : ITenantContext
    {
        public Guid TenantId { get; init; } = Guid.NewGuid();
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private static AppDbContext NewDb(ITenantContext tenant) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);

    private static CreateCommissionRuleCommand Valid() => new(Guid.NewGuid(), 10m, 0);

    [Fact]
    public void CreateValidator_rejects_negative_percentage()
    {
        var v = new CreateCommissionRuleValidator();

        Assert.False(v.Validate(Valid() with { Percentage = -1m }).IsValid);
        Assert.True(v.Validate(Valid() with { Percentage = 0m }).IsValid);
        Assert.True(v.Validate(Valid()).IsValid);
    }

    [Fact]
    public void UpdateValidator_rejects_negative_percentage()
    {
        var v = new UpdateCommissionRuleValidator();

        Assert.False(v.Validate(new UpdateCommissionRuleCommand(Guid.NewGuid(), -1m, 0)).IsValid);
        Assert.True(v.Validate(new UpdateCommissionRuleCommand(Guid.NewGuid(), 0m, 0)).IsValid);
    }

    [Fact]
    public async Task Create_then_update_then_delete_roundtrip()
    {
        var db = NewDb(new FixedTenant());
        var userId = Guid.NewGuid();

        var createHandler = new CreateCommissionRuleHandler(db);
        var createResult = await createHandler.Handle(new CreateCommissionRuleCommand(userId, 10m, 0), CancellationToken.None);
        Assert.True(createResult.IsSuccess);
        Assert.Equal(userId, createResult.Value.UserId);
        Assert.Equal(10m, createResult.Value.Percentage);
        var id = createResult.Value.Id;

        var updateHandler = new UpdateCommissionRuleHandler(db);
        var updateResult = await updateHandler.Handle(new UpdateCommissionRuleCommand(id, 15m, 1), CancellationToken.None);
        Assert.True(updateResult.IsSuccess);
        Assert.True(updateResult.Value);

        var listHandler = new ListCommissionRulesHandler(db);
        var listResult = await listHandler.Handle(new ListCommissionRulesQuery(1, 20), CancellationToken.None);
        Assert.True(listResult.IsSuccess);
        var updated = Assert.Single(listResult.Value.Items);
        Assert.Equal(15m, updated.Percentage);
        Assert.Equal(1, updated.Status);

        var deleteHandler = new DeleteCommissionRuleHandler(db);
        var deleteResult = await deleteHandler.Handle(new DeleteCommissionRuleCommand(id), CancellationToken.None);
        Assert.True(deleteResult.IsSuccess);

        var afterDelete = await listHandler.Handle(new ListCommissionRulesQuery(1, 20), CancellationToken.None);
        Assert.Empty(afterDelete.Value.Items);
    }

    [Fact]
    public async Task UpdateCommissionRuleHandler_returns_NotFound_for_missing_id()
    {
        var db = NewDb(new FixedTenant());
        var handler = new UpdateCommissionRuleHandler(db);

        var result = await handler.Handle(new UpdateCommissionRuleCommand(Guid.NewGuid(), 10m, 0), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }
}
