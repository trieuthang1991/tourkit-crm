using TourKit.Application.Common;
using TourKit.Application.Flights.Dtos;

namespace TourKit.Application.Flights;

public interface IFlightTicketIndividualService
{
    Task<PagedResult<FlightTicketIndividualDto>> ListAsync(int page, int size, FlightTicketIndividualListFilter? filter = null);
    Task<FlightTicketIndividualStatsDto> GetStatsAsync(FlightTicketIndividualListFilter? filter = null);
    Task<FlightTicketIndividualDto> GetAsync(Guid id);
    Task<FlightTicketIndividualDto> CreateAsync(CreateFlightTicketIndividualDto dto);
    Task UpdateAsync(Guid id, UpdateFlightTicketIndividualDto dto);
    Task DeleteAsync(Guid id);
}
