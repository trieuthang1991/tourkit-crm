using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Catalog.Validators;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests.Catalog;

/// <summary>Test <see cref="CarTypeService"/> qua fake <see cref="IRepository{T}"/> in-memory.</summary>
public class CarTypeServiceTests
{
    private static CarTypeService NewService(out FakeRepository<CarType> repo)
    {
        repo = new FakeRepository<CarType>();
        return new CarTypeService(repo, new CreateCarTypeValidator(), new UpdateCarTypeValidator());
    }

    [Fact]
    public async Task CreateAsync_returns_dto_and_persists()
    {
        var service = NewService(out var repo);

        var dto = await service.CreateAsync(new CreateCarTypeDto(45, "Xe 45 chỗ", 1));

        Assert.Equal(45, dto.Code);
        Assert.Equal("Xe 45 chỗ", dto.Name);
        Assert.NotNull(await repo.GetByIdAsync(dto.Id));
    }

    [Fact]
    public async Task CreateAsync_zero_code_throws_ValidationAppException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(new CreateCarTypeDto(0, "Xe", 1)));
    }

    [Fact]
    public async Task CreateAsync_duplicate_code_throws_ValidationAppException()
    {
        var service = NewService(out _);
        await service.CreateAsync(new CreateCarTypeDto(16, "Xe 16 chỗ", 1));

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(new CreateCarTypeDto(16, "Xe 16 chỗ khác", 2)));
    }

    [Fact]
    public async Task UpdateAsync_then_DeleteAsync_roundtrip()
    {
        var service = NewService(out var repo);
        var created = await service.CreateAsync(new CreateCarTypeDto(7, "Xe 7 chỗ", 1));

        await service.UpdateAsync(created.Id, new UpdateCarTypeDto(9, "Xe 9 chỗ", 2));
        var afterUpdate = (await service.ListAsync()).Single();
        Assert.Equal(9, afterUpdate.Code);
        Assert.Equal("Xe 9 chỗ", afterUpdate.Name);

        await service.DeleteAsync(created.Id);
        Assert.Null(await repo.GetByIdAsync(created.Id));
        Assert.Empty(await service.ListAsync());
    }

    [Fact]
    public async Task UpdateAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.UpdateAsync(Guid.NewGuid(), new UpdateCarTypeDto(4, "Xe 4 chỗ", 1)));
    }
}
