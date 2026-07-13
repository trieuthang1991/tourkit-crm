namespace TourKit.Application.Work;

public sealed record WorkTaskDto(
    Guid Id, string Title, string? Description, Guid? AssigneeUserId, string? AssigneeName,
    DateTimeOffset? DueDate, int Priority, int Status, Guid? RelatedOrderId,
    Guid? WorkflowId, Guid? SectionId);

/// <summary>Thẻ thống kê đầu màn Công việc: tổng · theo trạng thái (cần làm/đang làm/hoàn thành/huỷ) · quá hạn.</summary>
public sealed record WorkTaskStatsDto(int Total, int Todo, int InProgress, int Done, int Cancelled, int Overdue);

public sealed record CreateWorkTaskDto(
    string Title, string? Description, Guid? AssigneeUserId, DateTimeOffset? DueDate,
    int Priority, int Status, Guid? RelatedOrderId, Guid? WorkflowId, Guid? SectionId);

public sealed record UpdateWorkTaskDto(
    string Title, string? Description, Guid? AssigneeUserId, DateTimeOffset? DueDate,
    int Priority, int Status, Guid? RelatedOrderId, Guid? WorkflowId, Guid? SectionId);
