using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Catalog.Validators;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests.Catalog;

/// <summary>Test <see cref="LanguageTypeService"/> qua fake <see cref="IRepository{T}"/> in-memory.</summary>
public class LanguageTypeServiceTests
{
    private static LanguageTypeService NewService(out FakeRepository<LanguageType> repo)
    {
        repo = new FakeRepository<LanguageType>();
        return new LanguageTypeService(repo, new CreateLanguageTypeValidator(), new UpdateLanguageTypeValidator());
    }

    [Fact]
    public async Task CreateAsync_returns_dto_with_code_and_persists()
    {
        var service = NewService(out var repo);

        var dto = await service.CreateAsync(new CreateLanguageTypeDto("Tiếng Anh", "en", 1));

        Assert.Equal("Tiếng Anh", dto.Name);
        Assert.Equal("en", dto.Code);
        Assert.NotNull(await repo.GetByIdAsync(dto.Id));
    }

    [Fact]
    public async Task CreateAsync_empty_name_throws_ValidationAppException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(new CreateLanguageTypeDto("", null, 1)));
    }

    [Fact]
    public async Task CreateAsync_duplicate_name_throws_ValidationAppException()
    {
        var service = NewService(out _);
        await service.CreateAsync(new CreateLanguageTypeDto("Tiếng Trung", "zh", 1));

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(new CreateLanguageTypeDto("Tiếng Trung", "zh", 2)));
    }

    [Fact]
    public async Task UpdateAsync_then_DeleteAsync_roundtrip()
    {
        var service = NewService(out var repo);
        var created = await service.CreateAsync(new CreateLanguageTypeDto("Tiếng Nhật", "ja", 1));

        await service.UpdateAsync(created.Id, new UpdateLanguageTypeDto("Tiếng Hàn", "ko", 2));
        var afterUpdate = (await service.ListAsync()).Single();
        Assert.Equal("Tiếng Hàn", afterUpdate.Name);
        Assert.Equal("ko", afterUpdate.Code);

        await service.DeleteAsync(created.Id);
        Assert.Null(await repo.GetByIdAsync(created.Id));
        Assert.Empty(await service.ListAsync());
    }

    [Fact]
    public async Task UpdateAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.UpdateAsync(Guid.NewGuid(), new UpdateLanguageTypeDto("X", null, 1)));
    }
}
