using TourKit.Application.Catalog.Dtos;

namespace TourKit.Application.Catalog;

public interface ITourAssigneeService
{
    Task<IReadOnlyList<AssigneeDto>> ListAsync(Guid tourId);
    Task ReplaceAsync(Guid tourId, IReadOnlyList<AssigneeDto> assignees);
}
