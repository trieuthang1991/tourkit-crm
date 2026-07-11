using TourKit.Application.Booking.Dtos;

namespace TourKit.Application.Booking;

public interface ITourTransferService
{
    Task<TourTransferDto> TransferAsync(Guid orderId, TransferOrderDto dto);
    Task<IReadOnlyList<TourTransferDto>> ListByOrderAsync(Guid orderId);
}
