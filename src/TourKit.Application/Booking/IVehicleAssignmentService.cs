using TourKit.Application.Booking.Dtos;
using TourKit.Application.Common;

namespace TourKit.Application.Booking;

public interface IVehicleAssignmentService
{
    Task<PagedResult<VehicleAssignmentDto>> ListAsync(int page, int size, Guid? departureId);
    Task<VehicleAssignmentDto> CreateAsync(CreateVehicleAssignmentDto dto);
    Task UpdateAsync(Guid id, UpdateVehicleAssignmentDto dto);
    Task DeleteAsync(Guid id);
}
