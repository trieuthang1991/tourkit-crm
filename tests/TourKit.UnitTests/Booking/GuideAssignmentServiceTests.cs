using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Booking.Validators;
using TourKit.Application.Common;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.UnitTests.Booking;

public sealed class GuideAssignmentServiceTests
{
    private static async Task<(GuideAssignmentService Svc, Guid DepId, Guid ProvId)> NewServiceWithSeedAsync()
    {
        var repo = new FakeRepository<TourGuideAssignment>();
        var depRepo = new FakeRepository<TourDeparture>();
        var provRepo = new FakeRepository<Provider>();

        var depId = Guid.NewGuid();
        var provId = Guid.NewGuid();
        await depRepo.AddAsync(new TourDeparture { Id = depId });
        await depRepo.SaveChangesAsync();
        await provRepo.AddAsync(new Provider { Id = provId, Type = ProviderType.Guide });
        await provRepo.SaveChangesAsync();

        var svc = new GuideAssignmentService(
            repo, depRepo, provRepo,
            new CreateGuideAssignmentValidator(), new UpdateGuideAssignmentValidator());
        return (svc, depId, provId);
    }

    [Fact]
    public async Task CreateAsync_rejects_missing_departure()
    {
        var (svc, _, provId) = await NewServiceWithSeedAsync();

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            svc.CreateAsync(new CreateGuideAssignmentDto(Guid.NewGuid(), provId, null, null, null, null, 1)));
    }

    [Fact]
    public async Task CreateAsync_rejects_missing_provider()
    {
        var (svc, depId, _) = await NewServiceWithSeedAsync();

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            svc.CreateAsync(new CreateGuideAssignmentDto(depId, Guid.NewGuid(), null, null, null, null, 1)));
    }

    [Fact]
    public async Task CreateAsync_rejects_timecome_before_timego()
    {
        var (svc, depId, provId) = await NewServiceWithSeedAsync();
        var go = DateTimeOffset.UtcNow;
        var come = go.AddHours(-1);

        await Assert.ThrowsAsync<ValidationAppException>(() =>
            svc.CreateAsync(new CreateGuideAssignmentDto(depId, provId, go, come, null, null, 1)));
    }

    [Fact]
    public async Task Create_then_update_then_list_filter_then_delete_roundtrip()
    {
        var (svc, depId, provId) = await NewServiceWithSeedAsync();

        var created = await svc.CreateAsync(
            new CreateGuideAssignmentDto(depId, provId, null, null, null, "HDV chính", 1));
        Assert.Equal(depId, created.TourDepartureId);
        Assert.Equal(1, created.Status);

        await svc.UpdateAsync(created.Id,
            new UpdateGuideAssignmentDto(provId, null, null, null, "Đổi ghi chú", 2));

        var byDeparture = await svc.ListAsync(1, 20, depId);
        var one = Assert.Single(byDeparture.Items);
        Assert.Equal("Đổi ghi chú", one.Note);
        Assert.Equal(2, one.Status);

        var otherDeparture = await svc.ListAsync(1, 20, Guid.NewGuid());
        Assert.Empty(otherDeparture.Items);

        await svc.DeleteAsync(created.Id);
        var afterDelete = await svc.ListAsync(1, 20, null);
        Assert.Empty(afterDelete.Items);
    }

    [Fact]
    public async Task UpdateAsync_throws_NotFound_for_missing()
    {
        var (svc, _, provId) = await NewServiceWithSeedAsync();

        await Assert.ThrowsAsync<NotFoundException>(() =>
            svc.UpdateAsync(Guid.NewGuid(), new UpdateGuideAssignmentDto(provId, null, null, null, null, 1)));
    }

    [Fact]
    public async Task DeleteAsync_throws_NotFound_for_missing()
    {
        var (svc, _, _) = await NewServiceWithSeedAsync();

        await Assert.ThrowsAsync<NotFoundException>(() => svc.DeleteAsync(Guid.NewGuid()));
    }
}
