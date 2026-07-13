using TourKit.Application.Common;
using TourKit.Application.Crm.Dtos;

namespace TourKit.Application.Crm;

public interface ICustomerCareService
{
    Task<PagedResult<CustomerCareDto>> ListAsync(int page, int size, CustomerCareListFilter? filter = null);
    Task<CustomerCareStatsDto> GetStatsAsync();
    Task<CustomerCareDto> GetAsync(Guid id);
    Task<CustomerCareDto> CreateAsync(CreateCustomerCareDto dto);
    Task UpdateAsync(Guid id, UpdateCustomerCareDto dto);
    Task DeleteAsync(Guid id);
}
