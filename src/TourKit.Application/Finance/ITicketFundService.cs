using TourKit.Application.Common;
using TourKit.Application.Finance.Dtos;

namespace TourKit.Application.Finance;

public interface ITicketFundService
{
    Task<PagedResult<TicketFundDto>> ListAsync(int page, int size, TicketFundListFilter? filter = null);
    Task<TicketFundStatsDto> GetStatsAsync();
    Task<TicketFundDto> CreateAsync(CreateTicketFundDto dto);
    Task UpdateAsync(Guid id, UpdateTicketFundDto dto);

    /// <summary>Đổi nhanh trạng thái Chưa sử dụng/Đã sử dụng từ menu trên dòng lưới (không đụng IsClosed).</summary>
    Task SetStatusAsync(Guid id, int status);
    Task DeleteAsync(Guid id);
}
