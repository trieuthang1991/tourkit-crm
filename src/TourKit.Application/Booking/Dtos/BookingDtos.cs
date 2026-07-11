using TourKit.Shared.Enums;

namespace TourKit.Application.Booking.Dtos;

/// <summary>Yêu cầu đặt khách (dùng chung giữa "đặt chốt ngay" và "giữ chỗ").</summary>
public sealed record CreateBookingDto(Guid CustomerId, int AdultQty, int ChildQty, int ChildSmallQty, int BabyQty);

/// <summary>
/// Giá chỗ truyền tường minh (thay vì lấy từ mẫu tour) — dùng cho chuyến RIÊNG FIT không template:
/// giá theo hạng khách lấy từ BÁO GIÁ đã chốt (QuoteMath), không phải giá niêm yết.
/// </summary>
public sealed record SeatPrices(decimal Adult, decimal Child, decimal ChildSmall, decimal Baby);

public sealed record OrderDto(
    Guid Id, string Code, Guid TourDepartureId, Guid CustomerId, decimal TotalRevenue, decimal TotalCost,
    OrderStatus Status, Guid? SalesUserId);

public sealed record BookingLineDto(
    Guid Id, int Quantity, int AmountChildren, int AmountChildrenSmall, int QuantityBaby,
    decimal PriceAdult, decimal PriceChild, decimal PriceChildSmall, decimal PriceBaby,
    decimal UpfrontAmount, string? ReservationCode, bool IsMainContact);

public sealed record DepositDto(decimal Amount);

public sealed record CancelSeatDto(string? Note, decimal RefundAmount);

public sealed record SeatDto(
    Guid Id, Guid OrderId, SeatStatus Status, decimal UpfrontAmount, decimal LineTotal,
    DateTimeOffset? HoldExpiresAt, string? ReservationCode);

public sealed record AssignSalesDto(Guid? SalesUserId);
