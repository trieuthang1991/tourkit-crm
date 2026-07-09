using TourKit.Shared.Enums;

namespace TourKit.Application.Booking.Dtos;

/// <summary>Yêu cầu đặt khách (dùng chung giữa "đặt chốt ngay" và "giữ chỗ").</summary>
public sealed record CreateBookingDto(Guid CustomerId, int AdultQty, int ChildQty, int ChildSmallQty, int BabyQty);

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
