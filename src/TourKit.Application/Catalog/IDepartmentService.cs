using TourKit.Application.Catalog.Dtos;

namespace TourKit.Application.Catalog;

public interface IDepartmentService
{
    Task<IReadOnlyList<DepartmentDto>> ListAsync();
    Task<DepartmentDto> CreateAsync(CreateDepartmentDto dto);
    Task UpdateAsync(Guid id, UpdateDepartmentDto dto);
    Task DeleteAsync(Guid id);
}

public interface IPositionService
{
    Task<IReadOnlyList<PositionDto>> ListAsync();
    Task<PositionDto> CreateAsync(CreatePositionDto dto);
    Task UpdateAsync(Guid id, UpdatePositionDto dto);
    Task DeleteAsync(Guid id);
}
