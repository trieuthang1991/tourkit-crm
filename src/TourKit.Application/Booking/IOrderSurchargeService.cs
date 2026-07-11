using TourKit.Application.Booking.Dtos;

namespace TourKit.Application.Booking;

public interface IOrderSurchargeService
{
    Task<IReadOnlyList<OrderSurchargeDto>> ListByOrderAsync(Guid orderId);
    Task<OrderSurchargeDto> CreateAsync(Guid orderId, CreateOrderSurchargeDto dto);
    Task DeleteAsync(Guid orderId, Guid surchargeLineId);
}
