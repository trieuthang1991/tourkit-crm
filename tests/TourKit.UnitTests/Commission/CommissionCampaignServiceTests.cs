using TourKit.Application.Commission;
using TourKit.Application.Commission.Dtos;
using TourKit.Application.Commission.Validators;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.UnitTests.Commission;

/// <summary>
/// Test <see cref="CommissionCampaignService"/> qua fake <see cref="IRepository{T}"/> in-memory:
/// lưu header + nhân viên + bậc, replace-children khi sửa, chặn chồng thời gian theo nhân viên, tra cứu tỉ lệ.
/// </summary>
public class CommissionCampaignServiceTests
{
    private static CommissionCampaignService NewService(
        out FakeRepository<CommissionCampaign> repo,
        out FakeRepository<CommissionCampaignUser> userRepo,
        out FakeRepository<CommissionTier> tierRepo,
        out FakeRepository<User> accountRepo)
    {
        repo = new FakeRepository<CommissionCampaign>();
        userRepo = new FakeRepository<CommissionCampaignUser>();
        tierRepo = new FakeRepository<CommissionTier>();
        accountRepo = new FakeRepository<User>();
        return new CommissionCampaignService(
            repo, userRepo, tierRepo, accountRepo,
            new CreateCommissionCampaignValidator(), new UpdateCommissionCampaignValidator());
    }

    private static async Task<User> SeedUserAsync(FakeRepository<User> repo, string name = "Nguyễn Văn A")
    {
        var u = new User { Email = $"{Guid.NewGuid():N}@b.c", FullName = name };
        await repo.AddAsync(u);
        await repo.SaveChangesAsync();
        return u;
    }

    private static IReadOnlyList<CommissionTierInputDto> ThreeBands() =>
    [
        new(0m, 10_000_000m, 5m),
        new(10_000_000m, 50_000_000m, 10m),
        new(50_000_000m, 999_000_000m, 15m),
    ];

    private static DateTimeOffset D(int month, int day) => new(2026, month, day, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateAsync_persists_header_users_and_tiers()
    {
        var service = NewService(out _, out var userRepo, out var tierRepo, out var accountRepo);
        var u1 = await SeedUserAsync(accountRepo, "A");
        var u2 = await SeedUserAsync(accountRepo, "B");

        var detail = await service.CreateAsync(new CreateCommissionCampaignDto(
            "Q1 bậc thang", D(1, 1), D(3, 31), 0, [u1.Id, u2.Id], ThreeBands()));

        Assert.Equal("Q1 bậc thang", detail.Name);
        Assert.Equal(2, detail.UserIds.Count);
        Assert.Equal(3, detail.Tiers.Count);
        Assert.Equal(2, (await userRepo.ListAsync()).Count);
        Assert.Equal(3, (await tierRepo.ListAsync()).Count);
    }

    [Fact]
    public async Task CreateAsync_end_before_start_throws()
    {
        var service = NewService(out _, out _, out _, out var accountRepo);
        var u = await SeedUserAsync(accountRepo);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(
            new CreateCommissionCampaignDto("X", D(3, 31), D(1, 1), 0, [u.Id], ThreeBands())));
    }

    [Fact]
    public async Task CreateAsync_unknown_user_throws()
    {
        var service = NewService(out _, out _, out _, out _);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(
            new CreateCommissionCampaignDto("X", D(1, 1), D(3, 31), 0, [Guid.NewGuid()], ThreeBands())));
    }

    [Fact]
    public async Task CreateAsync_overlapping_active_campaign_same_user_throws()
    {
        var service = NewService(out _, out _, out _, out var accountRepo);
        var u = await SeedUserAsync(accountRepo);
        await service.CreateAsync(new CreateCommissionCampaignDto("Jan-Mar", D(1, 1), D(3, 31), 0, [u.Id], ThreeBands()));

        // Feb–Apr chồng Jan–Mar trên cùng nhân viên → chặn.
        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(
            new CreateCommissionCampaignDto("Feb-Apr", D(2, 1), D(4, 30), 0, [u.Id], ThreeBands())));
    }

    [Fact]
    public async Task CreateAsync_adjacent_ranges_same_user_allowed()
    {
        var service = NewService(out _, out _, out _, out var accountRepo);
        var u = await SeedUserAsync(accountRepo);
        await service.CreateAsync(new CreateCommissionCampaignDto("Jan-Mar", D(1, 1), D(3, 31), 0, [u.Id], ThreeBands()));

        // Apr–Jun không giao Jan–Mar → cho phép.
        var ok = await service.CreateAsync(new CreateCommissionCampaignDto("Apr-Jun", D(4, 1), D(6, 30), 0, [u.Id], ThreeBands()));
        Assert.NotEqual(Guid.Empty, ok.Id);
    }

    [Fact]
    public async Task CreateAsync_overlap_different_users_allowed()
    {
        var service = NewService(out _, out _, out _, out var accountRepo);
        var u1 = await SeedUserAsync(accountRepo, "A");
        var u2 = await SeedUserAsync(accountRepo, "B");
        await service.CreateAsync(new CreateCommissionCampaignDto("A pol", D(1, 1), D(3, 31), 0, [u1.Id], ThreeBands()));

        // Cùng thời gian nhưng nhân viên khác → không chồng.
        var ok = await service.CreateAsync(new CreateCommissionCampaignDto("B pol", D(1, 1), D(3, 31), 0, [u2.Id], ThreeBands()));
        Assert.NotEqual(Guid.Empty, ok.Id);
    }

    [Fact]
    public async Task CreateAsync_overlap_ignored_when_other_inactive()
    {
        var service = NewService(out _, out _, out _, out var accountRepo);
        var u = await SeedUserAsync(accountRepo);
        await service.CreateAsync(new CreateCommissionCampaignDto("Old", D(1, 1), D(3, 31), 1, [u.Id], ThreeBands()));

        // Chính sách cũ ở trạng thái ngừng (1) → không chặn chính sách mới trùng thời gian.
        var ok = await service.CreateAsync(new CreateCommissionCampaignDto("New", D(2, 1), D(4, 30), 0, [u.Id], ThreeBands()));
        Assert.NotEqual(Guid.Empty, ok.Id);
    }

    [Fact]
    public async Task UpdateAsync_replaces_users_and_tiers()
    {
        var service = NewService(out _, out var userRepo, out var tierRepo, out var accountRepo);
        var u1 = await SeedUserAsync(accountRepo, "A");
        var u2 = await SeedUserAsync(accountRepo, "B");
        var created = await service.CreateAsync(new CreateCommissionCampaignDto("P", D(1, 1), D(3, 31), 0, [u1.Id, u2.Id], ThreeBands()));

        await service.UpdateAsync(created.Id, new UpdateCommissionCampaignDto(
            "P2", D(1, 1), D(3, 31), 0, [u1.Id], [new(0m, 20_000_000m, 8m)]));

        var detail = await service.GetAsync(created.Id);
        Assert.Equal("P2", detail.Name);
        Assert.Single(detail.UserIds);
        Assert.Single(detail.Tiers);
        Assert.Single(await userRepo.ListAsync());
        Assert.Single(await tierRepo.ListAsync());
    }

    [Fact]
    public async Task UpdateAsync_can_keep_own_dates_without_self_overlap()
    {
        var service = NewService(out _, out _, out _, out var accountRepo);
        var u = await SeedUserAsync(accountRepo);
        var created = await service.CreateAsync(new CreateCommissionCampaignDto("P", D(1, 1), D(3, 31), 0, [u.Id], ThreeBands()));

        // Sửa chính nó, giữ nguyên thời gian → không được tự coi mình là chồng.
        await service.UpdateAsync(created.Id, new UpdateCommissionCampaignDto("P", D(1, 1), D(3, 31), 0, [u.Id], ThreeBands()));
        var detail = await service.GetAsync(created.Id);
        Assert.Equal("P", detail.Name);
    }

    [Fact]
    public async Task DeleteAsync_cascades_users_and_tiers()
    {
        var service = NewService(out var repo, out var userRepo, out var tierRepo, out var accountRepo);
        var u = await SeedUserAsync(accountRepo);
        var created = await service.CreateAsync(new CreateCommissionCampaignDto("P", D(1, 1), D(3, 31), 0, [u.Id], ThreeBands()));

        await service.DeleteAsync(created.Id);

        Assert.Null(await repo.GetByIdAsync(created.Id));
        Assert.Empty(await userRepo.ListAsync());
        Assert.Empty(await tierRepo.ListAsync());
    }

    [Fact]
    public async Task ResolveRateAsync_returns_band_rate_for_applicable_campaign()
    {
        var service = NewService(out _, out _, out _, out var accountRepo);
        var u = await SeedUserAsync(accountRepo);
        await service.CreateAsync(new CreateCommissionCampaignDto("P", D(1, 1), D(3, 31), 0, [u.Id], ThreeBands()));

        var result = await service.ResolveRateAsync(u.Id, D(2, 15), 30_000_000m);

        Assert.True(result.Found);
        Assert.Equal(10m, result.Rate);
        Assert.Equal(3_000_000m, result.Commission);
    }

    [Fact]
    public async Task ResolveRateAsync_not_found_when_date_outside_range()
    {
        var service = NewService(out _, out _, out _, out var accountRepo);
        var u = await SeedUserAsync(accountRepo);
        await service.CreateAsync(new CreateCommissionCampaignDto("P", D(1, 1), D(3, 31), 0, [u.Id], ThreeBands()));

        var result = await service.ResolveRateAsync(u.Id, D(6, 1), 30_000_000m);

        Assert.False(result.Found);
        Assert.Equal(0m, result.Rate);
    }
}
