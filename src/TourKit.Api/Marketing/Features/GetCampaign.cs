using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Marketing.Features;

public sealed record GetCampaignQuery(Guid Id) : IQuery<CampaignResponse>;

public sealed class GetCampaignHandler : IQueryHandler<GetCampaignQuery, CampaignResponse>
{
    private readonly AppDbContext _db;

    public GetCampaignHandler(AppDbContext db) => _db = db;

    public async Task<Result<CampaignResponse>> Handle(GetCampaignQuery q, CancellationToken ct)
    {
        var campaign = await _db.MarketingCampaigns.AsNoTracking()
            .Where(c => c.Id == q.Id)
            .Select(c => new CampaignResponse(c.Id, c.Name, c.Channel, c.Subject, c.Body, c.Status))
            .FirstOrDefaultAsync(ct);

        return campaign is null ? Error.NotFound() : campaign;
    }
}
