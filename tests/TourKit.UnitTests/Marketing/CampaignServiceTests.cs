using TourKit.Application.Common;
using TourKit.Application.Marketing;
using TourKit.Application.Marketing.Dtos;
using TourKit.Application.Marketing.Validators;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.UnitTests.Marketing;

/// <summary>
/// Test <see cref="CampaignService"/> qua fake <see cref="IRepository{T}"/> in-memory — nhanh,
/// KHÔNG EF, KHÔNG HTTP (cùng tinh thần với <c>ProviderServiceTests</c>).
/// </summary>
public class CampaignServiceTests
{
    private static CampaignService NewService(
        out FakeRepository<MarketingCampaign> repo, out FakeRepository<MarketingSendLog> logRepo)
    {
        repo = new FakeRepository<MarketingCampaign>();
        logRepo = new FakeRepository<MarketingSendLog>();
        return new CampaignService(repo, logRepo, new CreateCampaignValidator(), new UpdateCampaignValidator());
    }

    private static CreateCampaignDto NewCreateDto(string name = "Hè 2026") =>
        new(name, MarketingChannel.Email, "Chào hè", "Nội dung khuyến mãi hè 2026.");

    [Fact]
    public async Task CreateAsync_returns_dto_and_persists_entity()
    {
        var service = NewService(out var repo, out _);

        var dto = await service.CreateAsync(NewCreateDto());

        Assert.Equal("Hè 2026", dto.Name);
        Assert.Equal(MarketingChannel.Email, dto.Channel);
        var stored = await repo.GetByIdAsync(dto.Id);
        Assert.NotNull(stored);
        Assert.Equal("Hè 2026", stored!.Name);
    }

    [Fact]
    public async Task GetAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateAsync_empty_name_throws_ValidationAppException()
    {
        var service = NewService(out _, out _);

        await Assert.ThrowsAsync<ValidationAppException>(
            () => service.CreateAsync(new CreateCampaignDto("", MarketingChannel.Sms, null, "Body")));
    }

    [Fact]
    public async Task CreateAsync_empty_body_throws_ValidationAppException()
    {
        var service = NewService(out _, out _);

        await Assert.ThrowsAsync<ValidationAppException>(
            () => service.CreateAsync(new CreateCampaignDto("Ten", MarketingChannel.Sms, null, "")));
    }

    [Fact]
    public async Task UpdateAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.UpdateAsync(
            Guid.NewGuid(),
            new UpdateCampaignDto("Ten moi", MarketingChannel.Email, null, "Body", 2)));
    }

    [Fact]
    public async Task DeleteAsync_unknown_id_throws_NotFoundException()
    {
        var service = NewService(out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task SendAsync_unknown_campaign_throws_NotFoundException()
    {
        var service = NewService(out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.SendAsync(Guid.NewGuid(), new SendCampaignDto(["a@x.com"])));
    }

    [Fact]
    public async Task SendAsync_creates_a_log_per_recipient_and_marks_campaign_sent()
    {
        var service = NewService(out var repo, out var logRepo);
        var campaign = await service.CreateAsync(NewCreateDto());
        var recipients = new[] { "a@x.com", "b@x.com", "c@x.com" };

        var result = await service.SendAsync(campaign.Id, new SendCampaignDto(recipients));

        Assert.Equal(3, result.Sent);
        var logs = await logRepo.ListAsync(l => l.CampaignId == campaign.Id);
        Assert.Equal(3, logs.Count);
        var updated = await repo.GetByIdAsync(campaign.Id);
        Assert.Equal(1, updated!.Status);
    }

    [Fact]
    public async Task ListLogsAsync_returns_logs_for_campaign_only()
    {
        var service = NewService(out _, out _);
        var campaignA = await service.CreateAsync(NewCreateDto("A"));
        var campaignB = await service.CreateAsync(NewCreateDto("B"));
        await service.SendAsync(campaignA.Id, new SendCampaignDto(["a@x.com", "b@x.com"]));
        await service.SendAsync(campaignB.Id, new SendCampaignDto(["c@x.com"]));

        var logs = await service.ListLogsAsync(campaignA.Id);

        Assert.Equal(2, logs.Count);
        Assert.All(logs, l => Assert.Contains(l.Recipient, new[] { "a@x.com", "b@x.com" }));
    }
}
