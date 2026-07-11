using TourKit.Application.Common;
using TourKit.Application.Marketing;
using TourKit.Application.Marketing.Dtos;
using TourKit.Application.Marketing.Validators;
using TourKit.Application.Notifications;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.UnitTests.Marketing;

/// <summary>
/// Test <see cref="CampaignService"/> qua fake <see cref="IRepository{T}"/> in-memory — nhanh,
/// KHÔNG EF, KHÔNG HTTP (cùng tinh thần với <c>ProviderServiceTests</c>).
/// </summary>
public class CampaignServiceTests
{
    /// <summary>Fake email sender: đếm số email gửi; có thể mô phỏng lỗi cho địa chỉ nhất định.</summary>
    private sealed class FakeEmailSender(string? failFor = null) : IEmailSender
    {
        public List<string> Sent { get; } = [];
        public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
        {
            if (to == failFor)
            {
                throw new InvalidOperationException("SMTP từ chối");
            }

            Sent.Add(to);
            return Task.CompletedTask;
        }
    }

    /// <summary>Fake SMS sender: đếm số SMS gửi.</summary>
    private sealed class FakeSmsSender : ISmsSender
    {
        public List<string> Sent { get; } = [];
        public Task SendAsync(string phone, string message, CancellationToken ct = default)
        {
            Sent.Add(phone);
            return Task.CompletedTask;
        }
    }

    private static CampaignService NewService(
        out FakeRepository<MarketingCampaign> repo, out FakeRepository<MarketingSendLog> logRepo,
        IEmailSender? emailSender = null, ISmsSender? smsSender = null)
    {
        repo = new FakeRepository<MarketingCampaign>();
        logRepo = new FakeRepository<MarketingSendLog>();
        return new CampaignService(
            repo, logRepo, emailSender ?? new FakeEmailSender(), smsSender ?? new FakeSmsSender(),
            new CreateCampaignValidator(), new UpdateCampaignValidator());
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
    public async Task SendAsync_email_channel_sends_via_email_sender()
    {
        var sender = new FakeEmailSender();
        var service = NewService(out _, out var logRepo, sender);
        var campaign = await service.CreateAsync(NewCreateDto());   // kênh Email

        await service.SendAsync(campaign.Id, new SendCampaignDto(["a@x.com", "b@x.com"]));

        Assert.Equal(new[] { "a@x.com", "b@x.com" }, sender.Sent);   // đã gọi gửi thật
        var logs = await logRepo.ListAsync(l => l.CampaignId == campaign.Id);
        Assert.All(logs, l => Assert.Equal(1, l.Status));            // đều thành công
    }

    [Fact]
    public async Task SendAsync_email_failure_marks_that_recipient_failed_but_continues()
    {
        var sender = new FakeEmailSender(failFor: "bad@x.com");
        var service = NewService(out _, out var logRepo, sender);
        var campaign = await service.CreateAsync(NewCreateDto());

        var result = await service.SendAsync(campaign.Id, new SendCampaignDto(["a@x.com", "bad@x.com", "c@x.com"]));

        Assert.Equal(3, result.Sent);                               // vẫn ghi log cả 3
        Assert.Equal(new[] { "a@x.com", "c@x.com" }, sender.Sent);  // chỉ 2 địa chỉ tốt được gửi
        var logs = await logRepo.ListAsync(l => l.CampaignId == campaign.Id);
        Assert.Equal(1, logs.Count(l => l.Status == 2));            // 1 địa chỉ lỗi
        Assert.Equal(2, logs.Count(l => l.Status == 1));
    }

    [Fact]
    public async Task SendAsync_sms_channel_uses_sms_sender_not_email()
    {
        var email = new FakeEmailSender();
        var sms = new FakeSmsSender();
        var service = NewService(out _, out _, email, sms);
        var campaign = await service.CreateAsync(new CreateCampaignDto("SMS", MarketingChannel.Sms, null, "Nội dung"));

        await service.SendAsync(campaign.Id, new SendCampaignDto(["0900000000", "0911111111"]));

        Assert.Equal(new[] { "0900000000", "0911111111" }, sms.Sent);   // gửi qua SMS sender
        Assert.Empty(email.Sent);                                        // không gọi email
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
