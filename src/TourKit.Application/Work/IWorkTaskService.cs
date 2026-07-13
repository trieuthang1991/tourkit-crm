namespace TourKit.Application.Work;

public interface IWorkTaskService
{
    Task<IReadOnlyList<WorkTaskDto>> ListAsync(Guid? assigneeUserId, int? status, string? q = null, int? priority = null);
    Task<WorkTaskStatsDto> GetStatsAsync();
    Task<WorkTaskDto> CreateAsync(CreateWorkTaskDto dto);
    Task UpdateAsync(Guid id, UpdateWorkTaskDto dto);
    Task DeleteAsync(Guid id);
}
