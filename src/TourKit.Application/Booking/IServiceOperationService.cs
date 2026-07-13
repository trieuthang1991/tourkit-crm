using TourKit.Application.Booking.Dtos;
using TourKit.Application.Common;

namespace TourKit.Application.Booking;

public interface IServiceOperationService
{
    Task<PagedResult<ServiceOperationDto>> ListAsync(int page, int size, ServiceOperationListFilter? filter = null);
    Task<ServiceOperationStatsDto> GetStatsAsync(ServiceOperationListFilter? filter = null);
    Task<ServiceOperationDto> PayAsync(Guid id, PayServiceOperationDto dto);
}
