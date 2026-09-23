using TourKit.Application.Booking.Dtos;
using TourKit.Application.Common;

namespace TourKit.Application.Booking;

public interface IServiceBookingService
{
    Task<PagedResult<ServiceBookingDto>> ListAsync(int page, int size, ServiceBookingListFilter? filter = null);
    Task<ServiceBookingStatsDto> GetStatsAsync();
    Task<ServiceBookingDto> CreateAsync(CreateServiceBookingDto dto);
    Task UpdateAsync(Guid id, UpdateServiceBookingDto dto);
    /// <summary>Đổi nhanh trạng thái đặt dịch vụ (0 Chờ đặt·1 Đã đặt·2 Huỷ·3 Hoàn tất) theo state machine.</summary>
    Task SetStatusAsync(Guid id, int status);
    Task DeleteAsync(Guid id);
}
