using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Catalog.Validators;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests.Catalog;

/// <summary>Test <see cref="CurrencyService"/> (danh mục tỷ giá).</summary>
public class CurrencyServiceTests
{
    private static CurrencyService NewService(out FakeRepository<Currency> repo)
    {
        repo = new FakeRepository<Currency>();
        return new CurrencyService(repo, new CreateCurrencyValidator(), new UpdateCurrencyValidator());
    }

    [Fact]
    public async Task CreateAsync_uppercases_code_and_persists_rate()
    {
        var service = NewService(out _);

        var dto = await service.CreateAsync(new CreateCurrencyDto("usd", "Đô la Mỹ", 25_000m, 1));

        Assert.Equal("USD", dto.Code);
        Assert.Equal(25_000m, dto.RateToVnd);
    }

    [Fact]
    public async Task CreateAsync_zero_rate_throws()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(new CreateCurrencyDto("EUR", "Euro", 0m, 1)));
    }

    [Fact]
    public async Task CreateAsync_duplicate_code_throws_case_insensitive()
    {
        var service = NewService(out _);
        await service.CreateAsync(new CreateCurrencyDto("USD", "Đô la", 25_000m, 1));

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(new CreateCurrencyDto("usd", "Đô la khác", 24_000m, 2)));
    }

    [Fact]
    public async Task UpdateAsync_then_DeleteAsync_roundtrip()
    {
        var service = NewService(out var repo);
        var created = await service.CreateAsync(new CreateCurrencyDto("EUR", "Euro", 27_000m, 1));

        await service.UpdateAsync(created.Id, new UpdateCurrencyDto("EUR", "Euro", 28_000m, 2));
        Assert.Equal(28_000m, (await service.ListAsync()).Single().RateToVnd);

        await service.DeleteAsync(created.Id);
        Assert.Null(await repo.GetByIdAsync(created.Id));
    }
}
