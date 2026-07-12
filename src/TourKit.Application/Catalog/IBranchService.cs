using TourKit.Application.Catalog.Dtos;

namespace TourKit.Application.Catalog;

public interface IBranchService
{
    Task<IReadOnlyList<BranchDto>> ListAsync();
    Task<BranchDto> CreateAsync(CreateBranchDto dto);
    Task UpdateAsync(Guid id, UpdateBranchDto dto);
    Task DeleteAsync(Guid id);
}
