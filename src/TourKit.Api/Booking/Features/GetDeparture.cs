using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Booking.Features;

public sealed record GetDepartureQuery(Guid Id) : IQuery<DepartureResponse>;

public sealed class GetDepartureHandler : IQueryHandler<GetDepartureQuery, DepartureResponse>
{
    private readonly AppDbContext _db;

    public GetDepartureHandler(AppDbContext db) => _db = db;

    public async Task<Result<DepartureResponse>> Handle(GetDepartureQuery q, CancellationToken ct)
    {
        var departure = await _db.TourDepartures.AsNoTracking()
            .Where(x => x.Id == q.Id)
            .Select(x => new DepartureResponse(
                x.Id, x.Code, x.Title, x.ParentTourId, x.DepartureDate, x.EndDate, x.TotalSlots, x.Status))
            .FirstOrDefaultAsync(ct);

        return departure is null ? Error.NotFound() : departure;
    }
}
