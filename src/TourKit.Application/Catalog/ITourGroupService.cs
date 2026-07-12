using TourKit.Application.Catalog.Dtos;

namespace TourKit.Application.Catalog;

public interface ITourGroupService
{
    Task<IReadOnlyList<TourGroupDto>> ListAsync();
    Task<TourGroupDto> CreateAsync(CreateTourGroupDto dto);
    Task UpdateAsync(Guid id, UpdateTourGroupDto dto);
    Task DeleteAsync(Guid id);
}
