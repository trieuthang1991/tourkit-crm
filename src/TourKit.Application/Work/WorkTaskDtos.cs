namespace TourKit.Application.Work;

public sealed record WorkTaskDto(
    Guid Id, string Title, string? Description, Guid? AssigneeUserId, string? AssigneeName,
    DateTimeOffset? DueDate, int Priority, int Status, Guid? RelatedOrderId,
    Guid? WorkflowId, Guid? SectionId);

public sealed record CreateWorkTaskDto(
    string Title, string? Description, Guid? AssigneeUserId, DateTimeOffset? DueDate,
    int Priority, int Status, Guid? RelatedOrderId, Guid? WorkflowId, Guid? SectionId);

public sealed record UpdateWorkTaskDto(
    string Title, string? Description, Guid? AssigneeUserId, DateTimeOffset? DueDate,
    int Priority, int Status, Guid? RelatedOrderId, Guid? WorkflowId, Guid? SectionId);
