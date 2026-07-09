using TourKit.Application.Common;
using TourKit.Application.Crm;
using TourKit.Application.Crm.Dtos;
using TourKit.Application.Crm.Validators;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests.Crm;

/// <summary>
/// Test <see cref="TourRatingService"/> qua fake <see cref="IRepository{T}"/> in-memory — nhanh,
/// KHÔNG EF, KHÔNG HTTP.
/// </summary>
public class TourRatingServiceTests
{
    private static TourRatingService NewService(out FakeRepository<TourRating> repo)
    {
        repo = new FakeRepository<TourRating>();
        return new TourRatingService(repo, new CreateTourRatingValidator(), new UpdateTourRatingValidator());
    }

    private static CreateTourRatingDto NewCreateDto(int stars = 5) =>
        new(Guid.NewGuid(), null, "Nguyễn Văn A", "0900000000", stars, "Rất hài lòng", 0);

    [Fact]
    public async Task CreateAsync_returns_dto_and_persists_entity()
    {
        var service = NewService(out var repo);

        var dto = await service.CreateAsync(NewCreateDto());

        Assert.Equal(5, dto.Stars);
        var stored = await repo.GetByIdAsync(dto.Id);
        Assert.NotNull(stored);
        Assert.Equal(5, stored!.Stars);
    }

    [Fact]
    public async Task GetAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_stars_six_throws_ValidationAppException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(NewCreateDto(6)));
    }

    [Fact]
    public async Task CreateAsync_stars_zero_throws_ValidationAppException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(NewCreateDto(0)));
    }

    [Fact]
    public async Task UpdateAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(
            Guid.NewGuid(), new UpdateTourRatingDto("Tran Thi B", null, 3, null, 1)));
    }

    [Fact]
    public async Task UpdateAsync_stars_six_throws_ValidationAppException()
    {
        var service = NewService(out var repo);
        var created = await service.CreateAsync(NewCreateDto());

        await Assert.ThrowsAsync<ValidationAppException>(() => service.UpdateAsync(
            created.Id, new UpdateTourRatingDto("Tran Thi B", null, 6, null, 1)));
    }
}
