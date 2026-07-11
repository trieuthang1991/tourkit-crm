using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Catalog.Validators;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests.Catalog;

/// <summary>Test <see cref="RoomClassService"/> (danh mục hạng phòng).</summary>
public class RoomClassServiceTests
{
    private static RoomClassService NewService(out FakeRepository<RoomClass> repo)
    {
        repo = new FakeRepository<RoomClass>();
        return new RoomClassService(repo, new CreateRoomClassValidator(), new UpdateRoomClassValidator());
    }

    [Fact]
    public async Task Create_update_delete_roundtrip()
    {
        var service = NewService(out var repo);
        var created = await service.CreateAsync(new CreateRoomClassDto("Deluxe", 1));
        Assert.Equal("Deluxe", created.Name);

        await service.UpdateAsync(created.Id, new UpdateRoomClassDto("Suite", 2));
        Assert.Equal("Suite", (await service.ListAsync()).Single().Name);

        await service.DeleteAsync(created.Id);
        Assert.Null(await repo.GetByIdAsync(created.Id));
    }

    [Fact]
    public async Task Create_duplicate_name_throws()
    {
        var service = NewService(out _);
        await service.CreateAsync(new CreateRoomClassDto("Standard", 1));

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(new CreateRoomClassDto("Standard", 2)));
    }

    [Fact]
    public async Task Create_empty_name_throws()
    {
        var service = NewService(out _);
        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(new CreateRoomClassDto("", 1)));
    }
}
