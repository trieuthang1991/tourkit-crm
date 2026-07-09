using Microsoft.EntityFrameworkCore;
using TourKit.Shared.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Marketing.Features;

public sealed record SendCampaignCommand(Guid Id, string[] Recipients) : ICommand<SendResultResponse>;

public sealed class SendCampaignHandler : ICommandHandler<SendCampaignCommand, SendResultResponse>
{
    private readonly AppDbContext _db;

    public SendCampaignHandler(AppDbContext db) => _db = db;

    public async Task<Result<SendResultResponse>> Handle(SendCampaignCommand c, CancellationToken ct)
    {
        var campaign = await _db.MarketingCampaigns.FirstOrDefaultAsync(x => x.Id == c.Id, ct);
        if (campaign is null)
        {
            return Error.NotFound();
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var recipient in c.Recipients)
        {
            // Chưa tích hợp gửi thật (Email/SMS/Zalo provider) — chỉ ghi log; follow-up.
            _db.MarketingSendLogs.Add(new MarketingSendLog
            {
                CampaignId = c.Id, Recipient = recipient, Status = 1 /* sent-simulated */, SentAt = now,
            });
        }

        campaign.Status = 1; // đã gửi
        await _db.SaveChangesAsync(ct);

        return new SendResultResponse(c.Recipients.Length);
    }
}
