using TourKit.Application.Catalog.Dtos;

namespace TourKit.Application.Catalog;

public interface ICarTypeService
{
    Task<IReadOnlyList<CarTypeDto>> ListAsync();
    Task<CarTypeDto> CreateAsync(CreateCarTypeDto dto);
    Task UpdateAsync(Guid id, UpdateCarTypeDto dto);
    Task DeleteAsync(Guid id);
}
