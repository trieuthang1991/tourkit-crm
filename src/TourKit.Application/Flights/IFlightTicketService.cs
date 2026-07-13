using TourKit.Application.Common;
using TourKit.Application.Flights.Dtos;

namespace TourKit.Application.Flights;

public interface IFlightTicketService
{
    Task<PagedResult<FlightTicketDto>> ListAsync(int page, int size, FlightTicketListFilter? filter = null);
    Task<FlightTicketStatsDto> GetStatsAsync(FlightTicketListFilter? filter = null);
    Task<FlightTicketDto> GetAsync(Guid id);
    Task<FlightTicketDto> CreateAsync(CreateFlightTicketDto dto);
    Task UpdateAsync(Guid id, UpdateFlightTicketDto dto);
    Task<FlightTicketDto> AssignAsync(Guid id, AssignFlightTicketDto dto);
    Task DeleteAsync(Guid id);
}
