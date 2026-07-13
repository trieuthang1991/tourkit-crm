using TourKit.Application.Common;
using TourKit.Application.Sales.Dtos;

namespace TourKit.Application.Sales;

public interface IInvoiceService
{
    Task<PagedResult<InvoiceSummaryDto>> ListAsync(int page, int size, InvoiceListFilter? filter = null);
    Task<InvoiceStatsDto> GetStatsAsync();
    Task<InvoiceDto> GetAsync(Guid id);
    Task<InvoiceDto> CreateAsync(CreateInvoiceDto dto);
    Task<InvoiceDto> UpdateAsync(Guid id, UpdateInvoiceDto dto);
    Task DeleteAsync(Guid id);
}
