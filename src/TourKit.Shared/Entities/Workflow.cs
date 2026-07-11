namespace TourKit.Shared.Entities;

/// <summary>
/// Bảng quy trình/công việc (legacy <c>Workflow</c>): một "board" Kanban chứa các cột
/// <see cref="WorkflowSection"/> do người dùng tự định nghĩa (trạng thái theo dữ liệu, không hard-code).
/// Thẻ việc <see cref="WorkTask"/> tham chiếu board qua WorkflowId. Tự chứa, không phụ thuộc dịch vụ ngoài.
/// </summary>
public sealed class Workflow : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public int Status { get; set; }              // 0 đang dùng, 1 lưu trữ (legacy Workflow.Status)
}

/// <summary>
/// Cột trong board (legacy <c>SectionWork</c>): một trạng thái/giai đoạn do người dùng tự đặt.
/// Thứ tự theo <see cref="Sort"/>. Màu/biểu tượng để hiển thị. <see cref="AllowUpdate"/>/<see cref="AllowDelete"/>
/// cho phép khoá các cột hệ thống (vd cột "Hoàn thành") khỏi bị sửa/xoá.
/// </summary>
public sealed class WorkflowSection : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid WorkflowId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Sort { get; set; }
    public string? Color { get; set; }           // legacy TextColor
    public string? Icon { get; set; }
    public bool AllowUpdate { get; set; } = true;
    public bool AllowDelete { get; set; } = true;
}
