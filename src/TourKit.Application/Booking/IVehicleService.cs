using TourKit.Application.Booking.Dtos;
using TourKit.Application.Common;

namespace TourKit.Application.Booking;

public interface IVehicleService
{
    Task<PagedResult<VehicleDto>> ListAsync(int page, int size);
    Task<VehicleDto> CreateAsync(CreateVehicleDto dto);
    Task UpdateAsync(Guid id, UpdateVehicleDto dto);
    Task DeleteAsync(Guid id);
}
