using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Catalog.Validators;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests.Catalog;

/// <summary>Test <see cref="MarketTypeService"/> qua fake <see cref="IRepository{T}"/> in-memory.</summary>
public class MarketTypeServiceTests
{
    private static MarketTypeService NewService(out FakeRepository<MarketType> repo)
    {
        repo = new FakeRepository<MarketType>();
        return new MarketTypeService(repo, new CreateMarketTypeValidator(), new UpdateMarketTypeValidator());
    }

    [Fact]
    public async Task CreateAsync_returns_dto_and_persists_entity()
    {
        var service = NewService(out var repo);

        var dto = await service.CreateAsync(new CreateMarketTypeDto("Nội địa", null, 1));

        Assert.Equal("Nội địa", dto.Name);
        var stored = await repo.GetByIdAsync(dto.Id);
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task CreateAsync_empty_name_throws_ValidationAppException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<ValidationAppException>(
            () => service.CreateAsync(new CreateMarketTypeDto("", null, 1)));
    }

    [Fact]
    public async Task UpdateAsync_then_DeleteAsync_roundtrip()
    {
        var service = NewService(out var repo);
        var created = await service.CreateAsync(new CreateMarketTypeDto("Inbound", null, 1));

        await service.UpdateAsync(created.Id, new UpdateMarketTypeDto("Outbound", null, 2));
        var afterUpdate = (await service.ListAsync()).Single();
        Assert.Equal("Outbound", afterUpdate.Name);
        Assert.Equal(2, afterUpdate.SortOrder);

        await service.DeleteAsync(created.Id);
        Assert.Null(await repo.GetByIdAsync(created.Id));
        Assert.Empty(await service.ListAsync());
    }

    [Fact]
    public async Task UpdateAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.UpdateAsync(Guid.NewGuid(), new UpdateMarketTypeDto("X", null, 1)));
    }

    [Fact]
    public async Task DeleteAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(Guid.NewGuid()));
    }
}
