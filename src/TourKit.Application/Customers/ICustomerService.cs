using TourKit.Application.Common;

namespace TourKit.Application.Customers;

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> ListAsync(int page, int size, CancellationToken ct = default);
    Task<CustomerDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<CustomerDto> CreateAsync(CreateCustomerDto dto, CancellationToken ct = default);
    Task UpdateAsync(Guid id, UpdateCustomerDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
