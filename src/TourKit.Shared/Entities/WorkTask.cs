namespace TourKit.Shared.Entities;

/// <summary>
/// Công việc nội bộ (legacy <c>Tasking</c>/<c>UserInTasks</c>): giao/theo dõi việc trong tenant.
/// Tên WorkTask tránh trùng System.Threading.Tasks.Task. Không phụ thuộc dịch vụ ngoài.
/// </summary>
public sealed class WorkTask : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string? Code { get; set; }                  // Mã Task hiển thị (legacy #NNNNN) — idiom repo: "CV-XXXXXXXX"
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? AssigneeUserId { get; set; }          // người được giao (nullable)
    public Guid? CreatedByUserId { get; set; }         // người tạo (legacy Tasking.INS_UID) — dùng cho "Công việc của tôi"
    public DateTimeOffset? StartDate { get; set; }     // ngày bắt đầu (ngày nghiệp vụ, neo offset 0 qua TkDate.Day)
    public DateTimeOffset? DueDate { get; set; }
    public int Priority { get; set; }                  // WorkTaskPriority
    public int Progress { get; set; }                  // tiến độ % (0..100), legacy Tasking tiến độ
    public int Status { get; set; }                    // WorkTaskStatus
    public Guid? RelatedOrderId { get; set; }          // gắn với đơn (tuỳ chọn) — theo dõi việc theo đơn
    public Guid? WorkflowId { get; set; }              // board Kanban chứa thẻ việc (legacy Tasking.WorkflowId)
    public Guid? SectionId { get; set; }               // cột hiện tại trong board (kéo/thả đổi cột)
}
