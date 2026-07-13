using TourKit.Application.Booking.Dtos;
using TourKit.Application.Common;

namespace TourKit.Application.Booking;

public interface IServiceBookingService
{
    Task<PagedResult<ServiceBookingDto>> ListAsync(int page, int size, ServiceBookingListFilter? filter = null);
    Task<ServiceBookingStatsDto> GetStatsAsync();
    Task<ServiceBookingDto> CreateAsync(CreateServiceBookingDto dto);
    Task UpdateAsync(Guid id, UpdateServiceBookingDto dto);
    Task DeleteAsync(Guid id);
}
