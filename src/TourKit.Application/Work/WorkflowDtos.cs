namespace TourKit.Application.Work;

// Board (legacy Workflow)
public sealed record WorkflowDto(
    Guid Id, string Name, DateTimeOffset? StartDate, DateTimeOffset? EndDate, int Status,
    int SectionCount, int TaskCount);

public sealed record CreateWorkflowDto(string Name, DateTimeOffset? StartDate, DateTimeOffset? EndDate);
public sealed record UpdateWorkflowDto(string Name, DateTimeOffset? StartDate, DateTimeOffset? EndDate, int Status);

// Cột (legacy SectionWork)
public sealed record WorkflowSectionDto(
    Guid Id, Guid WorkflowId, string Name, int Sort, string? Color, string? Icon,
    bool AllowUpdate, bool AllowDelete);

public sealed record CreateSectionDto(string Name, string? Color, string? Icon);
public sealed record UpdateSectionDto(string Name, string? Color, string? Icon);
public sealed record ReorderSectionsDto(IReadOnlyList<Guid> SectionIds);
public sealed record MoveTaskDto(Guid SectionId);

// Chi tiết board: các cột (theo Sort) kèm thẻ việc trong mỗi cột
public sealed record BoardColumnDto(WorkflowSectionDto Section, IReadOnlyList<WorkTaskDto> Tasks);
public sealed record WorkflowBoardDto(Guid Id, string Name, int Status, IReadOnlyList<BoardColumnDto> Columns);
