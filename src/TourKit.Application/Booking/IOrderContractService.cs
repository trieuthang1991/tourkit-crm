using TourKit.Application.Booking.Dtos;

namespace TourKit.Application.Booking;

public interface IOrderContractService
{
    Task<OrderContractDto> GetAsync(Guid orderId);
}
