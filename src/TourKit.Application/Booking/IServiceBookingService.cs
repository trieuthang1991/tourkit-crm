using TourKit.Application.Booking.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Enums;

namespace TourKit.Application.Booking;

public interface IServiceBookingService
{
    Task<PagedResult<ServiceBookingDto>> ListAsync(int page, int size, ServiceBookingType? type, Guid? orderId);
    Task<ServiceBookingDto> CreateAsync(CreateServiceBookingDto dto);
    Task UpdateAsync(Guid id, UpdateServiceBookingDto dto);
    Task DeleteAsync(Guid id);
}
