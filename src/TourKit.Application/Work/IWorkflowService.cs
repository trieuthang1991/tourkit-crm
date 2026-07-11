namespace TourKit.Application.Work;

/// <summary>Board Kanban cấu hình động (legacy Workflow/SectionWork): board + cột + kéo thẻ việc.</summary>
public interface IWorkflowService
{
    Task<IReadOnlyList<WorkflowDto>> ListAsync();
    Task<WorkflowBoardDto> GetBoardAsync(Guid id);
    Task<WorkflowDto> CreateAsync(CreateWorkflowDto dto);
    Task UpdateAsync(Guid id, UpdateWorkflowDto dto);
    Task DeleteAsync(Guid id);

    Task<WorkflowSectionDto> AddSectionAsync(Guid workflowId, CreateSectionDto dto);
    Task UpdateSectionAsync(Guid workflowId, Guid sectionId, UpdateSectionDto dto);
    Task DeleteSectionAsync(Guid workflowId, Guid sectionId);
    Task ReorderSectionsAsync(Guid workflowId, ReorderSectionsDto dto);

    Task MoveTaskAsync(Guid workflowId, Guid taskId, MoveTaskDto dto);
}
