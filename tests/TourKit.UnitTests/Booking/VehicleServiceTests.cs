using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Booking.Validators;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests.Booking;

public sealed class VehicleServiceTests
{
    private static VehicleService NewService(FakeRepository<Vehicle>? repo = null)
        => new(repo ?? new FakeRepository<Vehicle>(), new CreateVehicleValidator(), new UpdateVehicleValidator());

    [Fact]
    public async Task CreateAsync_rejects_empty_name()
    {
        var service = NewService();

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            service.CreateAsync(new CreateVehicleDto("", "Hãng A", 16, 0)));
    }

    [Fact]
    public async Task Create_then_update_then_list_then_delete_roundtrip()
    {
        var repo = new FakeRepository<Vehicle>();
        var service = NewService(repo);

        var created = await service.CreateAsync(new CreateVehicleDto("Xe 16 chỗ", "Hãng A", 16, 0));
        Assert.Equal("Xe 16 chỗ", created.Name);

        await service.UpdateAsync(created.Id, new UpdateVehicleDto("Xe 29 chỗ", "Hãng B", 29, 1));

        var page = await service.ListAsync(1, 20);
        var updated = Assert.Single(page.Items);
        Assert.Equal("Xe 29 chỗ", updated.Name);
        Assert.Equal("Hãng B", updated.FirmName);
        Assert.Equal(29, updated.SeatType);
        Assert.Equal(1, updated.Status);

        await service.DeleteAsync(created.Id);
        var afterDelete = await service.ListAsync(1, 20);
        Assert.Empty(afterDelete.Items);
    }

    [Fact]
    public async Task UpdateAsync_throws_NotFound_for_missing_vehicle()
    {
        var service = NewService();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.UpdateAsync(Guid.NewGuid(), new UpdateVehicleDto("X", null, 4, 0)));
    }

    [Fact]
    public async Task DeleteAsync_throws_NotFound_for_missing_vehicle()
    {
        var service = NewService();

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(Guid.NewGuid()));
    }
}
