using TourKit.Application.Commission;
using TourKit.Application.Commission.Dtos;
using TourKit.Application.Commission.Validators;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests.Commission;

/// <summary>Test <see cref="CommissionRuleService"/> qua fake <see cref="IRepository{T}"/> in-memory.</summary>
public class CommissionRuleServiceTests
{
    private static CommissionRuleService NewService(out FakeRepository<CommissionRule> repo)
    {
        repo = new FakeRepository<CommissionRule>();
        return new CommissionRuleService(repo, new CreateCommissionRuleValidator(), new UpdateCommissionRuleValidator());
    }

    [Fact]
    public async Task CreateAsync_returns_dto_and_persists_entity()
    {
        var service = NewService(out var repo);
        var userId = Guid.NewGuid();

        var dto = await service.CreateAsync(new CreateCommissionRuleDto(userId, 10m, 0));

        Assert.Equal(userId, dto.UserId);
        Assert.Equal(10m, dto.Percentage);
        var stored = await repo.GetByIdAsync(dto.Id);
        Assert.NotNull(stored);
    }

    [Fact]
    public async Task CreateAsync_negative_percentage_throws_ValidationAppException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<ValidationAppException>(
            () => service.CreateAsync(new CreateCommissionRuleDto(Guid.NewGuid(), -1m, 0)));
    }

    [Fact]
    public async Task UpdateAsync_then_DeleteAsync_roundtrip()
    {
        var service = NewService(out var repo);
        var created = await service.CreateAsync(new CreateCommissionRuleDto(Guid.NewGuid(), 10m, 0));

        await service.UpdateAsync(created.Id, new UpdateCommissionRuleDto(15m, 1));
        var afterUpdate = (await service.ListAsync(1, 20)).Items.Single();
        Assert.Equal(15m, afterUpdate.Percentage);
        Assert.Equal(1, afterUpdate.Status);

        await service.DeleteAsync(created.Id);
        Assert.Null(await repo.GetByIdAsync(created.Id));
        Assert.Empty((await service.ListAsync(1, 20)).Items);
    }

    [Fact]
    public async Task UpdateAsync_negative_percentage_throws_ValidationAppException()
    {
        var service = NewService(out var repo);
        var created = await service.CreateAsync(new CreateCommissionRuleDto(Guid.NewGuid(), 10m, 0));

        await Assert.ThrowsAsync<ValidationAppException>(
            () => service.UpdateAsync(created.Id, new UpdateCommissionRuleDto(-1m, 0)));
        Assert.NotNull(await repo.GetByIdAsync(created.Id));
    }

    [Fact]
    public async Task UpdateAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.UpdateAsync(Guid.NewGuid(), new UpdateCommissionRuleDto(10m, 0)));
    }

    [Fact]
    public async Task DeleteAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ListAsync_returns_paged_result_with_total()
    {
        var service = NewService(out _);
        await service.CreateAsync(new CreateCommissionRuleDto(Guid.NewGuid(), 5m, 0));
        await service.CreateAsync(new CreateCommissionRuleDto(Guid.NewGuid(), 10m, 0));

        var page = await service.ListAsync(1, 20);

        Assert.Equal(2, page.Total);
        Assert.Equal(2, page.Items.Count);
    }
}
