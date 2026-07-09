using TourKit.Application.Common;
using TourKit.Application.Providers;
using TourKit.Application.Providers.Dtos;
using TourKit.Application.Providers.Validators;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests.Providers;

/// <summary>
/// Test <see cref="ServiceItemService"/> qua fake <see cref="IRepository{T}"/> in-memory — nhanh,
/// KHÔNG EF, KHÔNG HTTP (cùng tinh thần với <c>CustomerServiceTests</c>).
/// </summary>
public class ServiceItemServiceTests
{
    private static ServiceItemService NewService(out FakeRepository<ServiceItem> repo)
    {
        repo = new FakeRepository<ServiceItem>();
        return new ServiceItemService(repo, new CreateServiceItemValidator(), new UpdateServiceItemValidator());
    }

    [Fact]
    public async Task CreateAsync_returns_dto_and_persists_entity()
    {
        var service = NewService(out var repo);

        var dto = await service.CreateAsync(new CreateServiceItemDto("SVC-1", "Vé máy bay", 5, 1));

        Assert.Equal("SVC-1", dto.Code);
        Assert.Equal("Vé máy bay", dto.Name);
        var stored = await repo.GetByIdAsync(dto.Id);
        Assert.NotNull(stored);
        Assert.Equal("SVC-1", stored!.Code);
    }

    [Fact]
    public async Task GetAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_empty_name_throws_ValidationAppException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<ValidationAppException>(
            () => service.CreateAsync(new CreateServiceItemDto("SVC-2", "", 1, 1)));
    }

    [Fact]
    public async Task CreateAsync_duplicate_code_throws_ConflictException()
    {
        var service = NewService(out _);
        await service.CreateAsync(new CreateServiceItemDto("SVC-3", "Phòng khách sạn", 1, 1));

        await Assert.ThrowsAsync<ConflictException>(
            () => service.CreateAsync(new CreateServiceItemDto("SVC-3", "Phòng khác", 1, 1)));
    }

    [Fact]
    public async Task UpdateAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.UpdateAsync(Guid.NewGuid(), new UpdateServiceItemDto("Ten moi", 1, 1)));
    }
}
