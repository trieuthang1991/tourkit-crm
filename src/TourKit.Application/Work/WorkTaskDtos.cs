namespace TourKit.Application.Work;

public sealed record WorkTaskDto(
    Guid Id, string Title, string? Description, Guid? AssigneeUserId, string? AssigneeName,
    DateTimeOffset? DueDate, int Priority, int Status, Guid? RelatedOrderId,
    Guid? WorkflowId, Guid? SectionId,
    // Bám staging /tasking: cột "Người tạo" + "Dự án" (board). CreatedByName/WorkflowName resolve theo trang.
    Guid? CreatedByUserId = null, string? CreatedByName = null, string? WorkflowName = null,
    // Bám staging: Mã Task · Ngày bắt đầu · Tiến độ %.
    string? Code = null, DateTimeOffset? StartDate = null, int Progress = 0);

/// <summary>Thẻ thống kê đầu màn Công việc: tổng · theo trạng thái (cần làm/đang làm/hoàn thành/huỷ) · quá hạn.</summary>
public sealed record WorkTaskStatsDto(int Total, int Todo, int InProgress, int Done, int Cancelled, int Overdue);

public sealed record CreateWorkTaskDto(
    string Title, string? Description, Guid? AssigneeUserId, DateTimeOffset? DueDate,
    int Priority, int Status, Guid? RelatedOrderId, Guid? WorkflowId, Guid? SectionId,
    DateTimeOffset? StartDate = null, int Progress = 0);

public sealed record UpdateWorkTaskDto(
    string Title, string? Description, Guid? AssigneeUserId, DateTimeOffset? DueDate,
    int Priority, int Status, Guid? RelatedOrderId, Guid? WorkflowId, Guid? SectionId,
    DateTimeOffset? StartDate = null, int Progress = 0);
