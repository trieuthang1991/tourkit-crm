using TourKit.Application.Common;
using TourKit.Application.Sales.Dtos;

namespace TourKit.Application.Sales;

public interface IQuoteService
{
    Task<PagedResult<QuoteSummaryDto>> ListAsync(int page, int size, QuoteListFilter? filter = null);
    Task<QuoteStatsDto> GetStatsAsync(int? quoteType = null);
    Task<QuoteDto> GetAsync(Guid id);
    Task<QuoteDto> CreateAsync(CreateQuoteDto dto);
    Task<QuoteDto> UpdateAsync(Guid id, UpdateQuoteDto dto);
    /// <summary>Đổi nhanh trạng thái báo giá theo state machine (0 Nháp·1 Gửi·2 Chấp nhận·3 Từ chối).</summary>
    Task SetStatusAsync(Guid id, int status);
    Task DeleteAsync(Guid id);
}
