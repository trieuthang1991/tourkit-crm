using TourKit.Application.B2B.Dtos;
using TourKit.Application.Common;

namespace TourKit.Application.B2B;

public interface IAgentBookingService
{
    Task<PagedResult<AgentBookingSummaryDto>> ListAsync(int page, int size, Guid? agentId);
    Task<AgentBookingDto> GetAsync(Guid id);
    Task<AgentBookingDto> CreateFromQuoteAsync(CreateAgentBookingDto dto);
    Task UpdateStatusAsync(Guid id, int status);
    Task<AgentPassengerDto> AddPassengerAsync(Guid bookingId, AddAgentPassengerDto dto);
    Task RemovePassengerAsync(Guid bookingId, Guid passengerId);
}
