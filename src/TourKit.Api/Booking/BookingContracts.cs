
using TourKit.Shared.Enums;

namespace TourKit.Api.Booking;

public sealed record CreateBookingRequest(Guid CustomerId, int AdultQty, int ChildQty, int ChildSmallQty, int BabyQty);

public sealed record AssignSalesRequest(Guid? SalesUserId);

public sealed record OrderResponse(
    Guid Id, string Code, Guid TourDepartureId, Guid CustomerId, decimal TotalRevenue, decimal TotalCost,
    OrderStatus Status, Guid? SalesUserId);

public sealed record BookingLineResponse(
    Guid Id, int Quantity, int AmountChildren, int AmountChildrenSmall, int QuantityBaby,
    decimal PriceAdult, decimal PriceChild, decimal PriceChildSmall, decimal PriceBaby,
    decimal UpfrontAmount, string? ReservationCode, bool IsMainContact);

public sealed record DepositRequest(decimal Amount);

public sealed record CancelSeatRequest(string? Note, decimal RefundAmount);

public sealed record SeatResponse(
    Guid Id, Guid OrderId, SeatStatus Status, decimal UpfrontAmount, decimal LineTotal,
    DateTimeOffset? HoldExpiresAt, string? ReservationCode);
