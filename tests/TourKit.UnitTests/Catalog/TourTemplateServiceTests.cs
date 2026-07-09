using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Catalog.Validators;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests.Catalog;

/// <summary>
/// Test <see cref="TourTemplateService"/> (CRUD mẫu tour + replace-all lịch trình/bảng giá) qua fake
/// <see cref="IRepository{T}"/> in-memory — nhanh, KHÔNG EF, KHÔNG HTTP (cùng tinh thần với
/// <c>ProviderServiceTests</c>).
/// </summary>
public class TourTemplateServiceTests
{
    private static TourTemplateService NewService(
        out FakeRepository<TourTemplate> repo,
        out FakeRepository<TourItinerary> itineraryRepo,
        out FakeRepository<PriceScenario> priceScenarioRepo)
    {
        repo = new FakeRepository<TourTemplate>();
        itineraryRepo = new FakeRepository<TourItinerary>();
        priceScenarioRepo = new FakeRepository<PriceScenario>();
        return new TourTemplateService(
            repo, itineraryRepo, priceScenarioRepo,
            new CreateTourTemplateValidator(), new UpdateTourTemplateValidator());
    }

    private static CreateTourTemplateDto NewCreateDto(string code = "T-001") => new(
        code, "Đà Nẵng 3N2Đ", "domestic", 30, 24, 5_000_000m, 3_000_000m, 2_000_000m, 0m, "Điều khoản");

    [Fact]
    public async Task CreateAsync_returns_dto_and_persists_entity()
    {
        var service = NewService(out var repo, out _, out _);

        var dto = await service.CreateAsync(NewCreateDto());

        Assert.Equal("T-001", dto.Code);
        Assert.Equal(5_000_000m, dto.PriceAdult);
        var stored = await repo.GetByIdAsync(dto.Id);
        Assert.NotNull(stored);
        Assert.Equal("T-001", stored!.Code);
    }

    [Fact]
    public async Task GetAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _, out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_duplicate_code_throws_ConflictException()
    {
        var service = NewService(out _, out _, out _);
        await service.CreateAsync(NewCreateDto());

        await Assert.ThrowsAsync<ConflictException>(() => service.CreateAsync(NewCreateDto()));
    }

    [Fact]
    public async Task ReplaceItineraryAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _, out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.ReplaceItineraryAsync(
            Guid.NewGuid(), [new ItineraryDayDto(Guid.Empty, 1, "Ngày 1", null)]));
    }

    [Fact]
    public async Task ReplaceItineraryAsync_replaces_all_days_for_template()
    {
        var service = NewService(out _, out _, out _);
        var template = await service.CreateAsync(NewCreateDto());

        await service.ReplaceItineraryAsync(template.Id,
        [
            new ItineraryDayDto(Guid.Empty, 2, "Ngày 2", "Bà Nà Hills"),
            new ItineraryDayDto(Guid.Empty, 1, "Ngày 1", "Khởi hành"),
        ]);

        var days = await service.GetItineraryAsync(template.Id);
        Assert.Equal(2, days.Count);
        Assert.Equal(1, days[0].DayIndex);   // sắp xếp theo DayIndex
        Assert.Equal("Ngày 1", days[0].Title);

        // Lần thay thứ 2 phải XOÁ toàn bộ dòng cũ, không cộng dồn.
        await service.ReplaceItineraryAsync(template.Id, [new ItineraryDayDto(Guid.Empty, 1, "Ngày mới", null)]);
        var replaced = await service.GetItineraryAsync(template.Id);
        Assert.Single(replaced);
        Assert.Equal("Ngày mới", replaced[0].Title);
    }
}
