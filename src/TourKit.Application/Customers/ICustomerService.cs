using TourKit.Application.Common;
using TourKit.Application.Customers.Dtos;

namespace TourKit.Application.Customers;

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> ListAsync(int page, int size, string? q = null, int? customerType = null);
    Task<CustomerStatsDto> GetStatsAsync();
    Task<CustomerDto> GetAsync(Guid id);
    Task<CustomerDto> CreateAsync(CreateCustomerDto dto);
    Task UpdateAsync(Guid id, UpdateCustomerDto dto);
    Task DeleteAsync(Guid id);
}
