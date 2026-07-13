using TourKit.Application.Common;
using TourKit.Application.Finance.Dtos;

namespace TourKit.Application.Finance;

public interface ITicketFundService
{
    Task<PagedResult<TicketFundDto>> ListAsync(int page, int size, TicketFundListFilter? filter = null);
    Task<TicketFundStatsDto> GetStatsAsync();
    Task<TicketFundDto> CreateAsync(CreateTicketFundDto dto);
    Task UpdateAsync(Guid id, UpdateTicketFundDto dto);
    Task DeleteAsync(Guid id);
}
