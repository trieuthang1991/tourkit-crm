using TourKit.Application.B2B.Dtos;
using TourKit.Application.Common;

namespace TourKit.Application.B2B;

public interface IAgentService
{
    Task<PagedResult<AgentDto>> ListAsync(int page, int size);
    Task<AgentDto> CreateAsync(CreateAgentDto dto);
    Task UpdateAsync(Guid id, UpdateAgentDto dto);
    Task DeleteAsync(Guid id);
}
