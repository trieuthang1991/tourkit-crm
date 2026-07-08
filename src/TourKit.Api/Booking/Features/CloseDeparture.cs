using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Booking.Features;

/// <summary>Đóng chuyến (chốt sổ) — legacy StatusCloseTour. Đóng rồi không đặt thêm chỗ được.</summary>
public sealed record CloseDepartureCommand(Guid DepartureId) : ICommand<DepartureResponse>;

public sealed class CloseDepartureHandler : ICommandHandler<CloseDepartureCommand, DepartureResponse>
{
    private readonly AppDbContext _db;

    public CloseDepartureHandler(AppDbContext db) => _db = db;

    public async Task<Result<DepartureResponse>> Handle(CloseDepartureCommand c, CancellationToken ct)
    {
        var dep = await _db.TourDepartures.FirstOrDefaultAsync(d => d.Id == c.DepartureId, ct);
        if (dep is null)
        {
            return Error.NotFound();
        }

        if (dep.IsClosed)
        {
            return Error.Conflict("Chuyến đã đóng.");
        }

        dep.IsClosed = true;
        dep.ClosedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new DepartureResponse(
            dep.Id, dep.Code, dep.Title, dep.ParentTourId,
            dep.DepartureDate, dep.EndDate, dep.TotalSlots, dep.Status);
    }
}
