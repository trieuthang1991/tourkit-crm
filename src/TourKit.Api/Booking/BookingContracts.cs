using TourKit.Infrastructure.Entities;

namespace TourKit.Api.Booking;

public sealed record CreateBookingRequest(Guid CustomerId, int AdultQty, int ChildQty, int ChildSmallQty, int BabyQty);

public sealed record OrderResponse(
    Guid Id, string Code, Guid TourDepartureId, Guid CustomerId, decimal TotalRevenue, OrderStatus Status);

public sealed record BookingLineResponse(
    Guid Id, int Quantity, int AmountChildren, int AmountChildrenSmall, int QuantityBaby,
    decimal PriceAdult, decimal PriceChild, decimal PriceChildSmall, decimal PriceBaby,
    decimal UpfrontAmount, string? ReservationCode, bool IsMainContact);

/// <summary>Trạng thái chỗ (suy ra từ upfront_amount vs giá + timeRemaining — theo flow "Giữ chỗ" hệ cũ).</summary>
public enum SeatStatus
{
    Held = 1,           // giữ chỗ, còn đếm ngược (HoldExpiresAt != null, upfront = 0)
    HeldConfirmed = 2,  // chốt chỗ, không nhả (đã xác nhận chỗ → HoldExpiresAt = null, upfront = 0)
    Deposited = 3,      // đã đặt cọc (0 < upfront < tổng giá dòng)
    Paid = 4,           // đã thanh toán (upfront >= tổng giá dòng)
}

public sealed record DepositRequest(decimal Amount);

public sealed record SeatResponse(
    Guid Id, Guid OrderId, SeatStatus Status, decimal UpfrontAmount, decimal LineTotal,
    DateTimeOffset? HoldExpiresAt, string? ReservationCode);
