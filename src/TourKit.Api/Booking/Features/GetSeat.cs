using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Booking.Features;

public sealed record GetSeatQuery(Guid SeatId) : IQuery<SeatResponse>;

public sealed class GetSeatHandler : IQueryHandler<GetSeatQuery, SeatResponse>
{
    private readonly AppDbContext _db;

    public GetSeatHandler(AppDbContext db) => _db = db;

    public async Task<Result<SeatResponse>> Handle(GetSeatQuery q, CancellationToken ct)
    {
        var seat = await _db.TourCustomers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == q.SeatId, ct);
        return seat is null ? Error.NotFound() : SeatMapper.ToSeatResponse(seat);
    }
}
