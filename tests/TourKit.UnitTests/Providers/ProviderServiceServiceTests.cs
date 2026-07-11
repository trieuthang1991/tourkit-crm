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
        out FakeRepository<ProviderServiceEntity> repo, out FakeRepository<Provider> providerRepo,
        out FakeRepository<Currency> currencyRepo)
    {
        repo = new FakeRepository<ProviderServiceEntity>();
        providerRepo = new FakeRepository<Provider>();
        currencyRepo = new FakeRepository<Currency>();
        return new ProviderServiceService(
            repo, providerRepo, currencyRepo, new CreateProviderServiceValidator(), new UpdateProviderServiceValidator());
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
        var service = NewService(out var repo, out var providerRepo, out _);
        var provider = await SeedProviderAsync(providerRepo);

        var dto = await service.CreateAsync(new CreateProviderServiceDto(
            provider.Id, null, "Giá gốc", 1_000_000m, 1_200_000m, null, 2, null, 1));

        Assert.Equal(provider.Id, dto.ProviderId);
        Assert.Equal(1_000_000m, dto.ContractPrice);
        var stored = await repo.GetByIdAsync(dto.Id);
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task CreateAsync_vnd_default_has_vnd_equal_to_native()
    {
        var service = NewService(out _, out var providerRepo, out _);
        var provider = await SeedProviderAsync(providerRepo);

        // Không nhập tiền tệ → coi như VND, quy đổi = giá gốc.
        var dto = await service.CreateAsync(new CreateProviderServiceDto(
            provider.Id, null, "Giá VND", 1_000_000m, 1_200_000m, null, 2, null, 1));

        Assert.Equal(1_000_000m, dto.ContractPriceVnd);
        Assert.Equal(1_200_000m, dto.PublicPriceVnd);
    }

    [Fact]
    public async Task CreateAsync_foreign_currency_converts_to_vnd_by_rate()
    {
        var service = NewService(out _, out var providerRepo, out var currencyRepo);
        var provider = await SeedProviderAsync(providerRepo);
        await currencyRepo.AddAsync(new Currency { Code = "USD", Name = "Đô la Mỹ", RateToVnd = 25_000m });
        await currencyRepo.SaveChangesAsync();

        // Giá vốn 100 USD, tỷ giá 25.000 → 2.500.000 VND. CurrencyCode chuẩn hoá chữ hoa.
        var dto = await service.CreateAsync(new CreateProviderServiceDto(
            provider.Id, null, "Phòng KS nước ngoài", 100m, 120m, "usd", 2, null, 1));

        Assert.Equal("USD", dto.CurrencyCode);
        Assert.Equal(100m, dto.ContractPrice);           // giá gốc giữ nguyên ngoại tệ
        Assert.Equal(2_500_000m, dto.ContractPriceVnd);  // quy đổi VND
        Assert.Equal(3_000_000m, dto.PublicPriceVnd);
    }

    [Fact]
    public async Task GetAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _, out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_missing_provider_throws_ValidationAppException()
    {
        var service = NewService(out _, out _, out _);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(
            new CreateProviderServiceDto(Guid.NewGuid(), null, "Giá gốc", 1_000_000m, 1_200_000m, null, 2, null, 1)));
    }

    [Fact]
    public async Task UpdateAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _, out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(
            Guid.NewGuid(), new UpdateProviderServiceDto(null, "Giá mới", 1_000_000m, 1_200_000m, null, 2, null, 1)));
    }
}
