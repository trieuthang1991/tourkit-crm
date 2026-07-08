using TourKit.Infrastructure.Entities;

namespace TourKit.Api.Booking;

public sealed record CreateBookingRequest(Guid CustomerId, int AdultQty, int ChildQty, int ChildSmallQty, int BabyQty);

public sealed record OrderResponse(
    Guid Id, string Code, Guid TourDepartureId, Guid CustomerId, decimal TotalRevenue, OrderStatus Status);

public sealed record BookingLineResponse(
    Guid Id, int Quantity, int AmountChildren, int AmountChildrenSmall, int QuantityBaby,
    decimal PriceAdult, decimal PriceChild, decimal PriceChildSmall, decimal PriceBaby,
    decimal UpfrontAmount, string? ReservationCode, bool IsMainContact);
