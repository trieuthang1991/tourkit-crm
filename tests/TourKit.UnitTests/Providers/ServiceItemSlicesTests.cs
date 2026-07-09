using Microsoft.EntityFrameworkCore;
using TourKit.Api.Providers.Features;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;
using TourKit.Shared.Tenancy;

namespace TourKit.UnitTests.Providers;

/// <summary>
/// Test slice ServiceItem (danh mục dịch vụ) trực tiếp qua handler/validator — nhanh, KHÔNG HTTP,
/// KHÔNG server (cùng cách với <c>ProviderSlicesTests</c>).
/// </summary>
public class ServiceItemSlicesTests
{
    private sealed class FixedTenant : ITenantContext
    {
        public Guid TenantId { get; init; } = Guid.NewGuid();
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private static AppDbContext NewDb(ITenantContext tenant) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);

    private static CreateServiceItemCommand Valid() => new("SV-1", "Phòng đôi tiêu chuẩn", 1, 1);

    [Fact]
    public void Validator_rejects_empty_code_or_name()
    {
        var v = new CreateServiceItemValidator();

        Assert.False(v.Validate(Valid() with { Code = "" }).IsValid);
        Assert.False(v.Validate(Valid() with { Name = "" }).IsValid);
        Assert.True(v.Validate(Valid()).IsValid);
    }

    [Fact]
    public async Task CreateServiceItemHandler_creates_and_returns_response()
    {
        var db = NewDb(new FixedTenant());
        var handler = new CreateServiceItemHandler(db);

        var result = await handler.Handle(Valid(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("SV-1", result.Value.Code);
        Assert.Equal(1, await db.ServiceItems.CountAsync());
    }

    [Fact]
    public async Task CreateServiceItemHandler_returns_Conflict_on_duplicate_code()
    {
        var db = NewDb(new FixedTenant());
        await new CreateServiceItemHandler(db).Handle(Valid(), CancellationToken.None);

        var result = await new CreateServiceItemHandler(db).Handle(Valid(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Conflict, result.Error!.Type);
    }

    [Fact]
    public async Task Create_then_update_then_delete_roundtrip()
    {
        var db = NewDb(new FixedTenant());

        var createResult = await new CreateServiceItemHandler(db).Handle(Valid(), CancellationToken.None);
        Assert.True(createResult.IsSuccess);
        var id = createResult.Value.Id;

        var updateHandler = new UpdateServiceItemHandler(db);
        var updateResult = await updateHandler.Handle(
            new UpdateServiceItemCommand(id, "Phòng đôi cao cấp", 1, 0), CancellationToken.None);
        Assert.True(updateResult.IsSuccess);
        Assert.True(updateResult.Value);

        var listHandler = new ListServiceItemsHandler(db);
        var listResult = await listHandler.Handle(new ListServiceItemsQuery(1, 20), CancellationToken.None);
        Assert.True(listResult.IsSuccess);
        var updated = Assert.Single(listResult.Value.Items);
        Assert.Equal("Phòng đôi cao cấp", updated.Name);
        Assert.Equal(0, updated.Status);

        var deleteHandler = new DeleteServiceItemHandler(db);
        var deleteResult = await deleteHandler.Handle(new DeleteServiceItemCommand(id), CancellationToken.None);
        Assert.True(deleteResult.IsSuccess);

        var afterDelete = await listHandler.Handle(new ListServiceItemsQuery(1, 20), CancellationToken.None);
        Assert.Empty(afterDelete.Value.Items);
    }

    [Fact]
    public async Task UpdateServiceItemHandler_returns_NotFound_for_missing_id()
    {
        var db = NewDb(new FixedTenant());
        var handler = new UpdateServiceItemHandler(db);

        var result = await handler.Handle(
            new UpdateServiceItemCommand(Guid.NewGuid(), "X", 1, 0), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }
}
