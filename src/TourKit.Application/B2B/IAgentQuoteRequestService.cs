using TourKit.Application.B2B.Dtos;
using TourKit.Application.Common;

namespace TourKit.Application.B2B;

public interface IAgentQuoteRequestService
{
    Task<PagedResult<AgentQuoteRequestDto>> ListAsync(int page, int size, AgentQuoteRequestListFilter? filter = null);
    Task<AgentQuoteStatsDto> GetStatsAsync();
    Task<AgentQuoteRequestDto> GetAsync(Guid id);
    Task<AgentQuoteRequestDto> CreateAsync(CreateAgentQuoteRequestDto dto);   // Đại lý gửi yêu cầu
    Task<AgentQuoteRequestDto> QuoteAsync(Guid id, QuoteAgentRequestDto dto); // Sales chào giá → Quoted
    Task<AgentQuoteRequestDto> ConfirmAsync(Guid id);                         // Đại lý xác nhận → Confirmed
    Task<AgentQuoteRequestDto> RejectAsync(Guid id, string? note);            // → Rejected
}
