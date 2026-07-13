using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Booking.Validators;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests.Booking;

public sealed class VehicleAssignmentServiceTests
{
    private static async Task<(VehicleAssignmentService Svc, Guid DepId, Guid VehId)> NewServiceWithSeedAsync()
    {
        var repo = new FakeRepository<VehicleAssignment>();
        var depRepo = new FakeRepository<TourDeparture>();
        var vehRepo = new FakeRepository<Vehicle>();

        var depId = Guid.NewGuid();
        var vehId = Guid.NewGuid();
        await depRepo.AddAsync(new TourDeparture { Id = depId });
        await depRepo.SaveChangesAsync();
        await vehRepo.AddAsync(new Vehicle { Id = vehId, Name = "Xe 45 chỗ", SeatType = 45 });
        await vehRepo.SaveChangesAsync();

        var svc = new VehicleAssignmentService(
            repo, depRepo, vehRepo,
            new CreateVehicleAssignmentValidator(), new UpdateVehicleAssignmentValidator());
        return (svc, depId, vehId);
    }

    [Fact]
    public async Task CreateAsync_rejects_missing_departure()
    {
        var (svc, _, vehId) = await NewServiceWithSeedAsync();

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            svc.CreateAsync(new CreateVehicleAssignmentDto(Guid.NewGuid(), vehId, null, null, null, null, null, 1)));
    }

    [Fact]
    public async Task CreateAsync_rejects_missing_vehicle()
    {
        var (svc, depId, _) = await NewServiceWithSeedAsync();

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            svc.CreateAsync(new CreateVehicleAssignmentDto(depId, Guid.NewGuid(), null, null, null, null, null, 1)));
    }

    [Fact]
    public async Task CreateAsync_rejects_timecome_before_timego()
    {
        var (svc, depId, vehId) = await NewServiceWithSeedAsync();
        var go = DateTimeOffset.UtcNow;
        var come = go.AddHours(-1);

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            svc.CreateAsync(new CreateVehicleAssignmentDto(depId, vehId, null, null, go, come, null, 1)));
    }

    [Fact]
    public async Task Create_then_update_then_list_filter_then_delete_roundtrip()
    {
        var (svc, depId, vehId) = await NewServiceWithSeedAsync();

        var created = await svc.CreateAsync(
            new CreateVehicleAssignmentDto(depId, vehId, "Anh Ba", "0900000000", null, null, "Xe chính", 1));
        Assert.Equal(depId, created.TourDepartureId);
        Assert.Equal("Anh Ba", created.DriverName);
        Assert.Equal(1, created.Status);

        await svc.UpdateAsync(created.Id,
            new UpdateVehicleAssignmentDto(vehId, "Anh Tư", "0911111111", null, null, "Đổi tài xế", 2));

        var byDeparture = await svc.ListAsync(1, 20, new VehicleAssignmentListFilter(DepartureId: depId));
        var one = Assert.Single(byDeparture.Items);
        Assert.Equal("Anh Tư", one.DriverName);
        Assert.Equal("Đổi tài xế", one.Note);
        Assert.Equal(2, one.Status);

        var otherDeparture = await svc.ListAsync(1, 20, new VehicleAssignmentListFilter(DepartureId: Guid.NewGuid()));
        Assert.Empty(otherDeparture.Items);

        await svc.DeleteAsync(created.Id);
        var afterDelete = await svc.ListAsync(1, 20);
        Assert.Empty(afterDelete.Items);
    }

    [Fact]
    public async Task UpdateAsync_throws_NotFound_for_missing()
    {
        var (svc, _, vehId) = await NewServiceWithSeedAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            svc.UpdateAsync(Guid.NewGuid(), new UpdateVehicleAssignmentDto(vehId, null, null, null, null, null, 1)));
    }

    [Fact]
    public async Task DeleteAsync_throws_NotFound_for_missing()
    {
        var (svc, _, _) = await NewServiceWithSeedAsync();

        await Assert.ThrowsAsync<NotFoundException>(() => svc.DeleteAsync(Guid.NewGuid()));
    }
}
