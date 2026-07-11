using TourKit.Application.Catalog.Dtos;

namespace TourKit.Application.Catalog;

public interface ICustomerSourceService
{
    Task<IReadOnlyList<CustomerSourceDto>> ListAsync();
    Task<CustomerSourceDto> CreateAsync(CreateCustomerSourceDto dto);
    Task UpdateAsync(Guid id, UpdateCustomerSourceDto dto);
    Task DeleteAsync(Guid id);
}
