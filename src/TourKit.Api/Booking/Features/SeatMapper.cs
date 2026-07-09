using TourKit.Shared.Domain;
using TourKit.Shared.Entities;

using TourKit.Shared.Enums;

namespace TourKit.Api.Booking.Features;

/// <summary>Chiếu TourCustomer (chỗ) → SeatResponse + suy trạng thái — dùng chung giữa các slice chỗ.</summary>
internal static class SeatMapper
{
    public static SeatResponse ToSeatResponse(TourCustomer s)
    {
        var lineTotal = BookingMath.LineTotal(s);   // công thức 1 chỗ
        return new SeatResponse(s.Id, s.OrderId, DeriveStatus(s, lineTotal),
            s.UpfrontAmount, lineTotal, s.HoldExpiresAt, s.ReservationCode);
    }

    // Suy trạng thái theo bảng flow "Giữ chỗ" hệ cũ.
    public static SeatStatus DeriveStatus(TourCustomer s, decimal lineTotal)
    {
        if (s.Status != 0)
        {
            return SeatStatus.Cancelled;
        }

        if (s.UpfrontAmount >= lineTotal && lineTotal > 0m)
        {
            return SeatStatus.Paid;
        }

        if (s.UpfrontAmount > 0m)
        {
            return SeatStatus.Deposited;
        }

        return s.HoldExpiresAt is not null ? SeatStatus.Held : SeatStatus.HeldConfirmed;
    }
}
