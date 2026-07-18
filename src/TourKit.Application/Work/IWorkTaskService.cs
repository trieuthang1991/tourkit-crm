using TourKit.Application.Common;

namespace TourKit.Application.Work;

public interface IWorkTaskService
{
    Task<PagedResult<WorkTaskDto>> ListAsync(
        int page, int size, Guid? assigneeUserId, int? status, string? q = null, int? priority = null);
    Task<WorkTaskStatsDto> GetStatsAsync();
    Task<WorkTaskDto> CreateAsync(CreateWorkTaskDto dto);
    Task UpdateAsync(Guid id, UpdateWorkTaskDto dto);
    Task DeleteAsync(Guid id);
}
