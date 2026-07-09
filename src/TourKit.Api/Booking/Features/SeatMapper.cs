using TourKit.Shared.Domain;
using TourKit.Shared.Entities;

namespace TourKit.Api.Booking.Features;

/// <summary>Chiếu TourCustomer (chỗ) → SeatResponse. Công thức tiền &amp; suy trạng thái nằm ở BookingMath (một chỗ).</summary>
internal static class SeatMapper
{
    public static SeatResponse ToSeatResponse(TourCustomer s)
        => new(s.Id, s.OrderId, BookingMath.DeriveSeatStatus(s),
            s.UpfrontAmount, BookingMath.LineTotal(s), s.HoldExpiresAt, s.ReservationCode);
}
