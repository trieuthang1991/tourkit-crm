using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Booking.Features;

/// <summary>Giữ chỗ: Order Draft, upfront = 0, HoldExpiresAt = now + ReservationHours (đếm ngược).</summary>
public sealed record CreateHoldCommand(
    Guid DepartureId, Guid CustomerId, int AdultQty, int ChildQty, int ChildSmallQty, int BabyQty)
    : ICommand<SeatResponse>;

public sealed class CreateHoldHandler : ICommandHandler<CreateHoldCommand, SeatResponse>
{
    private readonly AppDbContext _db;

    public CreateHoldHandler(AppDbContext db) => _db = db;

    public async Task<Result<SeatResponse>> Handle(CreateHoldCommand c, CancellationToken ct)
    {
        var result = await BookingFactory.BuildAsync(
            _db, c.DepartureId, c.CustomerId, c.AdultQty, c.ChildQty, c.ChildSmallQty, c.BabyQty,
            isHold: true, ct);
        if (result.IsFailure)
        {
            return result.Error!;
        }

        return SeatMapper.ToSeatResponse(result.Value.Seat);
    }
}
