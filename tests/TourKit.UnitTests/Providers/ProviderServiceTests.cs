using TourKit.Application.Common;
using TourKit.Application.Providers.Dtos;
using TourKit.Application.Providers.Validators;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;
using ProviderCrudService = TourKit.Application.Providers.ProviderService;

namespace TourKit.UnitTests.Providers;

/// <summary>
/// Test <see cref="TourKit.Application.Providers.ProviderService"/> (CRUD nhà cung cấp) qua fake
/// <see cref="IRepository{T}"/> in-memory — nhanh, KHÔNG EF, KHÔNG HTTP (cùng tinh thần với
/// <c>CustomerServiceTests</c>). Dùng bí danh <c>ProviderCrudService</c> vì tên trùng với entity
/// <see cref="TourKit.Shared.Entities.ProviderService"/> khi cả 2 namespace cùng được using.
/// </summary>
public class ProviderServiceTests
{
    private static ProviderCrudService NewService(out FakeRepository<Provider> repo)
    {
        repo = new FakeRepository<Provider>();
        return new ProviderCrudService(repo, new CreateProviderValidator(), new UpdateProviderValidator());
    }

    private static CreateProviderDto NewCreateDto(string code = "NCC-1") => new(
        code, "Khách sạn ABC", ProviderType.Hotel, "0900000000", "abc@ncc.vn", "123 Đường A",
        "0101234567", "Nguyen Van B", "1234567890", "Vietcombank", null, 4, 1);

    [Fact]
    public async Task CreateAsync_returns_dto_and_persists_entity()
    {
        var service = NewService(out var repo);

        var dto = await service.CreateAsync(NewCreateDto());

        Assert.Equal("NCC-1", dto.Code);
        Assert.Equal("Khách sạn ABC", dto.Name);
        var stored = await repo.GetByIdAsync(dto.Id);
        Assert.NotNull(stored);
        Assert.Equal("NCC-1", stored!.Code);
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
            () => service.CreateAsync(new CreateProviderDto(
                "NCC-2", "", ProviderType.Hotel, null, null, null, null, null, null, null, null, 0, 1)));
    }

    [Fact]
    public async Task CreateAsync_duplicate_code_throws_ConflictException()
    {
        var service = NewService(out _);
        await service.CreateAsync(NewCreateDto());

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(NewCreateDto()));
    }

    [Fact]
    public async Task UpdateAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(
            Guid.NewGuid(),
            new UpdateProviderDto("Ten moi", ProviderType.Hotel, null, null, null, null, null, null, null, null, 0, 1)));
    }
}
