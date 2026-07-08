using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Booking.Features;

public sealed record ListDeparturesQuery(int Page, int Size) : IQuery<Paged<DepartureResponse>>;

public sealed class ListDeparturesHandler : IQueryHandler<ListDeparturesQuery, Paged<DepartureResponse>>
{
    private readonly AppDbContext _db;

    public ListDeparturesHandler(AppDbContext db) => _db = db;

    public async Task<Result<Paged<DepartureResponse>>> Handle(ListDeparturesQuery q, CancellationToken ct)
    {
        var page = new PageQuery(q.Page, q.Size);
        var baseQuery = _db.TourDepartures.AsNoTracking().OrderBy(d => d.DepartureDate);

        var total = await baseQuery.CountAsync(ct);
        var items = await baseQuery
            .Skip(page.Skip).Take(page.SafeSize)
            .Select(d => new DepartureResponse(
                d.Id, d.Code, d.Title, d.ParentTourId, d.DepartureDate, d.EndDate, d.TotalSlots, d.Status))
            .ToListAsync(ct);

        return new Paged<DepartureResponse>(items, total, page.SafePage, page.SafeSize);
    }
}
