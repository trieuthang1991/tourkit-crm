using Microsoft.EntityFrameworkCore;
using TourKit.Api.Marketing.Features;
using TourKit.Shared.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;
using TourKit.Shared.Tenancy;

namespace TourKit.UnitTests.Marketing;

/// <summary>
/// Test slice Marketing trực tiếp qua handler/validator — nhanh, KHÔNG HTTP, KHÔNG server
/// (cùng cách với <c>CustomerSlicesTests</c>/<c>ProviderSlicesTests</c>).
/// </summary>
public class MarketingSlicesTests
{
    private sealed class FixedTenant : ITenantContext
    {
        public Guid TenantId { get; init; } = Guid.NewGuid();
        public bool HasTenant => TenantId != Guid.Empty;
    }

    private static AppDbContext NewDb(ITenantContext tenant) =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, tenant);

    private static CreateCampaignCommand Valid() =>
        new("Hè 2026", MarketingChannel.Email, "Chào hè", "Nội dung khuyến mãi hè 2026.");

    [Fact]
    public void Validator_rejects_empty_name_or_body()
    {
        var v = new CreateCampaignValidator();

        Assert.False(v.Validate(Valid() with { Name = "" }).IsValid);
        Assert.False(v.Validate(Valid() with { Body = "" }).IsValid);
        Assert.True(v.Validate(Valid()).IsValid);
    }

    [Fact]
    public async Task SendCampaignHandler_returns_NotFound_for_missing_campaign()
    {
        var db = NewDb(new FixedTenant());
        var handler = new SendCampaignHandler(db);

        var result = await handler.Handle(
            new SendCampaignCommand(Guid.NewGuid(), ["a@x.com"]), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.NotFound, result.Error!.Type);
    }

    [Fact]
    public async Task SendCampaignHandler_creates_a_log_per_recipient_and_marks_campaign_sent()
    {
        var tenant = new FixedTenant();
        var db = NewDb(tenant);
        var campaign = new MarketingCampaign
        {
            TenantId = tenant.TenantId, Name = "Hè 2026", Channel = MarketingChannel.Email,
            Body = "Nội dung", Status = 0,
        };
        db.MarketingCampaigns.Add(campaign);
        await db.SaveChangesAsync();

        var handler = new SendCampaignHandler(db);
        var recipients = new[] { "a@x.com", "b@x.com", "c@x.com" };

        var result = await handler.Handle(new SendCampaignCommand(campaign.Id, recipients), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Sent);
        Assert.Equal(3, await db.MarketingSendLogs.CountAsync(l => l.CampaignId == campaign.Id));
        Assert.Equal(1, (await db.MarketingCampaigns.FirstAsync(c => c.Id == campaign.Id)).Status);
    }
}
