using TourKit.Application.Catalog.Dtos;

namespace TourKit.Application.Catalog;

public interface ICustomerTagService
{
    Task<IReadOnlyList<CustomerTagDto>> ListAsync();
    Task<CustomerTagDto> CreateAsync(CreateCustomerTagDto dto);
    Task UpdateAsync(Guid id, UpdateCustomerTagDto dto);
    Task DeleteAsync(Guid id);
}
