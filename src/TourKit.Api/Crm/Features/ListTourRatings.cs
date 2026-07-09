using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Crm.Features;

public sealed record ListTourRatingsQuery(int Page, int Size) : IQuery<Paged<TourRatingResponse>>;

public sealed class ListTourRatingsHandler : IQueryHandler<ListTourRatingsQuery, Paged<TourRatingResponse>>
{
    private readonly AppDbContext _db;

    public ListTourRatingsHandler(AppDbContext db) => _db = db;

    public async Task<Result<Paged<TourRatingResponse>>> Handle(ListTourRatingsQuery q, CancellationToken ct)
    {
        var page = new PageQuery(q.Page, q.Size);
        var baseQuery = _db.TourRatings.AsNoTracking().OrderByDescending(r => r.CreatedAt);

        var total = await baseQuery.CountAsync(ct);
        var items = await baseQuery
            .Skip(page.Skip).Take(page.SafeSize)
            .Select(r => new TourRatingResponse(
                r.Id, r.TourDepartureId, r.OrderId, r.CustomerName, r.CustomerPhone, r.Stars, r.Comment, r.Status))
            .ToListAsync(ct);

        return new Paged<TourRatingResponse>(items, total, page.SafePage, page.SafeSize);
    }
}
