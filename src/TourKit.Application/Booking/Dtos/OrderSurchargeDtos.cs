namespace TourKit.Application.Booking.Dtos;

public sealed record OrderSurchargeDto(
    Guid Id, Guid OrderId, Guid? SurchargeId, string Description, int CalcType, decimal Value, decimal Amount);

public sealed record CreateOrderSurchargeDto(Guid? SurchargeId, string Description, int CalcType, decimal Value);
