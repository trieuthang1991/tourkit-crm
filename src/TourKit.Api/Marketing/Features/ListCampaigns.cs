using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Marketing.Features;

public sealed record ListCampaignsQuery(int Page, int Size) : IQuery<Paged<CampaignResponse>>;

public sealed class ListCampaignsHandler : IQueryHandler<ListCampaignsQuery, Paged<CampaignResponse>>
{
    private readonly AppDbContext _db;

    public ListCampaignsHandler(AppDbContext db) => _db = db;

    public async Task<Result<Paged<CampaignResponse>>> Handle(ListCampaignsQuery q, CancellationToken ct)
    {
        var page = new PageQuery(q.Page, q.Size);
        var baseQuery = _db.MarketingCampaigns.AsNoTracking().OrderByDescending(c => c.CreatedAt);

        var total = await baseQuery.CountAsync(ct);
        var items = await baseQuery
            .Skip(page.Skip).Take(page.SafeSize)
            .Select(c => new CampaignResponse(c.Id, c.Name, c.Channel, c.Subject, c.Body, c.Status))
            .ToListAsync(ct);

        return new Paged<CampaignResponse>(items, total, page.SafePage, page.SafeSize);
    }
}
