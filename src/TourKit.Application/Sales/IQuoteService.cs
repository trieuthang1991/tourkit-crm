using TourKit.Application.Common;
using TourKit.Application.Sales.Dtos;

namespace TourKit.Application.Sales;

public interface IQuoteService
{
    Task<PagedResult<QuoteSummaryDto>> ListAsync(int page, int size);
    Task<QuoteDto> GetAsync(Guid id);
    Task<QuoteDto> CreateAsync(CreateQuoteDto dto);
    Task<QuoteDto> UpdateAsync(Guid id, UpdateQuoteDto dto);
    Task DeleteAsync(Guid id);
}
