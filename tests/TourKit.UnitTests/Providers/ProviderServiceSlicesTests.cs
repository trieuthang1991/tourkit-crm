using Microsoft.EntityFrameworkCore;
using TourKit.Api.Providers.Features;
using TourKit.Shared.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;
using TourKit.Shared.Tenancy;

namespace TourKit.UnitTests.Providers;

/// <summary>
/// Test slice ProviderService (bảng giá dịch vụ theo NCC) trực tiếp qua handler/validator — nhanh,
/// KHÔNG HTTP, KHÔNG server (cùng cách với <c>ProviderSlicesTests</c>).
/// </summary>
public class ProviderServiceSlicesTests
{
    private sealed class FixedTenant : ITenantContext
    {
        public Guid TenantId { get; init; } = Guid.NewGuid();
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private static AppDbContext NewDb(ITenantContext tenant) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);

    private static CreateProviderServiceCommand Valid(Guid providerId) => new(
        providerId, null, "Gói tiêu chuẩn", 1_000_000m, 1_200_000m, 2, null, 1);

    [Fact]
    public async Task CreateProviderServiceHandler_returns_Validation_for_missing_provider()
    {
        var db = NewDb(new FixedTenant());
        var handler = new CreateProviderServiceHandler(db);

        var result = await handler.Handle(Valid(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error!.Type);
    }

    [Fact]
    public async Task Create_then_update_then_delete_roundtrip()
    {
        var tenant = new FixedTenant();
        var db = NewDb(tenant);
        var provider = new Provider { Code = "NCC-1", Name = "Khách sạn ABC", TenantId = tenant.TenantId };
        db.Providers.Add(provider);
        await db.SaveChangesAsync();

        var createHandler = new CreateProviderServiceHandler(db);
        var createResult = await createHandler.Handle(Valid(provider.Id), CancellationToken.None);
        Assert.True(createResult.IsSuccess);
        Assert.Equal(provider.Id, createResult.Value.ProviderId);
        var id = createResult.Value.Id;

        var updateHandler = new UpdateProviderServiceHandler(db);
        var updateResult = await updateHandler.Handle(
            new UpdateProviderServiceCommand(id, null, "Gói VIP", 1_500_000m, 1_800_000m, 4, "Ghi chú", 0),
            CancellationToken.None);
        Assert.True(updateResult.IsSuccess);
        Assert.True(updateResult.Value);

        var listHandler = new ListProviderServicesHandler(db);
        var listResult = await listHandler.Handle(new ListProviderServicesQuery(1, 20, null), CancellationToken.None);
        Assert.True(listResult.IsSuccess);
        var updated = Assert.Single(listResult.Value.Items);
        Assert.Equal("Gói VIP", updated.PriceName);
        Assert.Equal(1_500_000m, updated.ContractPrice);
        Assert.Equal(0, updated.Status);

        var deleteHandler = new DeleteProviderServiceHandler(db);
        var deleteResult = await deleteHandler.Handle(new DeleteProviderServiceCommand(id), CancellationToken.None);
        Assert.True(deleteResult.IsSuccess);

        var afterDelete = await listHandler.Handle(new ListProviderServicesQuery(1, 20, null), CancellationToken.None);
        Assert.Empty(afterDelete.Value.Items);
    }

    [Fact]
    public async Task ListProviderServicesHandler_filters_by_providerId()
    {
        var tenant = new FixedTenant();
        var db = NewDb(tenant);
        var providerA = new Provider { Code = "NCC-A", Name = "NCC A", TenantId = tenant.TenantId };
        var providerB = new Provider { Code = "NCC-B", Name = "NCC B", TenantId = tenant.TenantId };
        db.Providers.AddRange(providerA, providerB);
        await db.SaveChangesAsync();

        var createHandler = new CreateProviderServiceHandler(db);
        await createHandler.Handle(Valid(providerA.Id), CancellationToken.None);
        await createHandler.Handle(Valid(providerB.Id), CancellationToken.None);
        await createHandler.Handle(Valid(providerA.Id), CancellationToken.None);

        var listHandler = new ListProviderServicesHandler(db);
        var result = await listHandler.Handle(new ListProviderServicesQuery(1, 20, providerA.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Total);
        Assert.All(result.Value.Items, x => Assert.Equal(providerA.Id, x.ProviderId));
    }

    [Fact]
    public async Task UpdateProviderServiceHandler_returns_NotFound_for_missing_id()
    {
        var db = NewDb(new FixedTenant());
        var handler = new UpdateProviderServiceHandler(db);

        var result = await handler.Handle(
            new UpdateProviderServiceCommand(Guid.NewGuid(), null, "X", 0m, 0m, 0, null, 0), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }
}
