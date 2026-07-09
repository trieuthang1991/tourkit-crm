using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Booking.Features;

public sealed record ListVehiclesQuery(int Page, int Size) : IQuery<Paged<VehicleResponse>>;

public sealed class ListVehiclesHandler : IQueryHandler<ListVehiclesQuery, Paged<VehicleResponse>>
{
    private readonly AppDbContext _db;

    public ListVehiclesHandler(AppDbContext db) => _db = db;

    public async Task<Result<Paged<VehicleResponse>>> Handle(ListVehiclesQuery q, CancellationToken ct)
    {
        var page = new PageQuery(q.Page, q.Size);
        var baseQuery = _db.Vehicles.AsNoTracking().OrderBy(v => v.CreatedAt);

        var total = await baseQuery.CountAsync(ct);
        var items = await baseQuery
            .Skip(page.Skip).Take(page.SafeSize)
            .Select(v => new VehicleResponse(v.Id, v.Name, v.FirmName, v.SeatType, v.Status))
            .ToListAsync(ct);

        return new Paged<VehicleResponse>(items, total, page.SafePage, page.SafeSize);
    }
}
