using TourKit.Application.Catalog.Dtos;

namespace TourKit.Application.Catalog;

public interface ICustomerTypeService
{
    Task<IReadOnlyList<CustomerTypeDto>> ListAsync();
    Task<CustomerTypeDto> CreateAsync(CreateCustomerTypeDto dto);
    Task UpdateAsync(Guid id, UpdateCustomerTypeDto dto);
    Task DeleteAsync(Guid id);
}
