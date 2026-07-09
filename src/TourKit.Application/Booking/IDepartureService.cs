using TourKit.Application.Booking.Dtos;
using TourKit.Application.Common;

namespace TourKit.Application.Booking;

public interface IDepartureService
{
    Task<PagedResult<DepartureDto>> ListAsync(int page, int size);
    Task<DepartureDto> GetAsync(Guid id);
    Task<DepartureDto> CreateAsync(CreateDepartureDto dto);
    Task<DepartureDto> CloseAsync(Guid id);
}
