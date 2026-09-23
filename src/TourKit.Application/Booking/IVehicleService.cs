using TourKit.Application.Booking.Dtos;
using TourKit.Application.Common;

namespace TourKit.Application.Booking;

public interface IVehicleService
{
    Task<PagedResult<VehicleDto>> ListAsync(int page, int size, string? q = null);
    Task<VehicleDto> CreateAsync(CreateVehicleDto dto);
    Task UpdateAsync(Guid id, UpdateVehicleDto dto);

    /// <summary>Đổi nhanh trạng thái Hoạt động/Ngừng từ menu trên dòng lưới (không đụng các trường khác).</summary>
    Task SetStatusAsync(Guid id, int status);
    Task DeleteAsync(Guid id);
}
