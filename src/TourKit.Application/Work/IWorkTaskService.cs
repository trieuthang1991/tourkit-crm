namespace TourKit.Application.Work;

public interface IWorkTaskService
{
    Task<IReadOnlyList<WorkTaskDto>> ListAsync(Guid? assigneeUserId, int? status);
    Task<WorkTaskDto> CreateAsync(CreateWorkTaskDto dto);
    Task UpdateAsync(Guid id, UpdateWorkTaskDto dto);
    Task DeleteAsync(Guid id);
}
