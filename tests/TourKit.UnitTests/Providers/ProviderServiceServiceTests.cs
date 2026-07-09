using TourKit.Application.Common;
using TourKit.Application.Providers;
using TourKit.Application.Providers.Dtos;
using TourKit.Application.Providers.Validators;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;
using ProviderServiceEntity = TourKit.Shared.Entities.ProviderService;

namespace TourKit.UnitTests.Providers;

/// <summary>
/// Test <see cref="ProviderServiceService"/> (bảng giá dịch vụ theo NCC) qua fake
/// <see cref="IRepository{T}"/> in-memory — nhanh, KHÔNG EF, KHÔNG HTTP.
/// Dùng bí danh <c>ProviderServiceEntity</c> vì entity trùng tên với service <see cref="TourKit.Application.Providers.ProviderService"/>
/// (service CRUD của Provider) khi cả 2 namespace cùng được using.
/// </summary>
public class ProviderServiceServiceTests
{
    private static ProviderServiceService NewService(
        out FakeRepository<ProviderServiceEntity> repo, out FakeRepository<Provider> providerRepo)
    {
        repo = new FakeRepository<ProviderServiceEntity>();
        providerRepo = new FakeRepository<Provider>();
        return new ProviderServiceService(
            repo, providerRepo, new CreateProviderServiceValidator(), new UpdateProviderServiceValidator());
    }

    private static async Task<Provider> SeedProviderAsync(FakeRepository<Provider> providerRepo)
    {
        var provider = new Provider { Code = "NCC-1", Name = "Khách sạn ABC", Type = ProviderType.Hotel, Status = 1 };
        await providerRepo.AddAsync(provider);
        await providerRepo.SaveChangesAsync();
        return provider;
    }

    [Fact]
    public async Task CreateAsync_returns_dto_and_persists_entity()
    {
        var service = NewService(out var repo, out var providerRepo);
        var provider = await SeedProviderAsync(providerRepo);

        var dto = await service.CreateAsync(new CreateProviderServiceDto(
            provider.Id, null, "Giá gốc", 1_000_000m, 1_200_000m, 2, null, 1));

        Assert.Equal(provider.Id, dto.ProviderId);
        Assert.Equal(1_000_000m, dto.ContractPrice);
        var stored = await repo.GetByIdAsync(dto.Id);
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task GetAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_missing_provider_throws_ValidationAppException()
    {
        var service = NewService(out _, out _);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(
            new CreateProviderServiceDto(Guid.NewGuid(), null, "Giá gốc", 1_000_000m, 1_200_000m, 2, null, 1)));
    }

    [Fact]
    public async Task UpdateAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(
            Guid.NewGuid(), new UpdateProviderServiceDto(null, "Giá mới", 1_000_000m, 1_200_000m, 2, null, 1)));
    }
}
