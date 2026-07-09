using Microsoft.EntityFrameworkCore;
using TourKit.Api.Booking.Features;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;
using TourKit.Shared.Tenancy;

namespace TourKit.UnitTests.Booking;

/// <summary>
/// Test slice Vehicle (xe) trực tiếp qua handler/validator — nhanh, KHÔNG HTTP, KHÔNG server
/// (cùng cách với <c>ProviderServiceSlicesTests</c>).
/// </summary>
public class VehicleSlicesTests
{
    private sealed class FixedTenant : ITenantContext
    {
        public Guid TenantId { get; init; } = Guid.NewGuid();
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private static AppDbContext NewDb(ITenantContext tenant) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);

    private static CreateVehicleCommand Valid() => new("Xe 45 chỗ", "Phương Trang", 45, 1);

    [Fact]
    public async Task CreateVehicleHandler_returns_Validation_for_empty_name()
    {
        var db = NewDb(new FixedTenant());
        var handler = new CreateVehicleHandler(db);
        var validator = new CreateVehicleValidator();

        var command = new CreateVehicleCommand(string.Empty, null, 16, 1);
        var validation = await validator.ValidateAsync(command);

        Assert.False(validation.IsValid);
    }

    [Fact]
    public async Task Create_then_update_then_delete_roundtrip()
    {
        var tenant = new FixedTenant();
        var db = NewDb(tenant);

        var createHandler = new CreateVehicleHandler(db);
        var createResult = await createHandler.Handle(Valid(), CancellationToken.None);
        Assert.True(createResult.IsSuccess);
        Assert.Equal("Xe 45 chỗ", createResult.Value.Name);
        var id = createResult.Value.Id;

        var updateHandler = new UpdateVehicleHandler(db);
        var updateResult = await updateHandler.Handle(
            new UpdateVehicleCommand(id, "Xe 29 chỗ", "Mai Linh", 29, 0), CancellationToken.None);
        Assert.True(updateResult.IsSuccess);
        Assert.True(updateResult.Value);

        var listHandler = new ListVehiclesHandler(db);
        var listResult = await listHandler.Handle(new ListVehiclesQuery(1, 20), CancellationToken.None);
        Assert.True(listResult.IsSuccess);
        var updated = Assert.Single(listResult.Value.Items);
        Assert.Equal("Xe 29 chỗ", updated.Name);
        Assert.Equal("Mai Linh", updated.FirmName);
        Assert.Equal(29, updated.SeatType);
        Assert.Equal(0, updated.Status);

        var deleteHandler = new DeleteVehicleHandler(db);
        var deleteResult = await deleteHandler.Handle(new DeleteVehicleCommand(id), CancellationToken.None);
        Assert.True(deleteResult.IsSuccess);

        var afterDelete = await listHandler.Handle(new ListVehiclesQuery(1, 20), CancellationToken.None);
        Assert.Empty(afterDelete.Value.Items);
    }

    [Fact]
    public async Task UpdateVehicleHandler_returns_NotFound_for_missing_id()
    {
        var db = NewDb(new FixedTenant());
        var handler = new UpdateVehicleHandler(db);

        var result = await handler.Handle(
            new UpdateVehicleCommand(Guid.NewGuid(), "X", null, 0, 0), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task DeleteVehicleHandler_returns_NotFound_for_missing_id()
    {
        var db = NewDb(new FixedTenant());
        var handler = new DeleteVehicleHandler(db);

        var result = await handler.Handle(new DeleteVehicleCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }
}
