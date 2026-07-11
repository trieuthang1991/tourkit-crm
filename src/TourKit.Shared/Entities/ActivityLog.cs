namespace TourKit.Shared.Entities;

/// <summary>
/// Nhật ký thao tác (audit) — grounded ở legacy bảng <c>ActivityLogs</c>
/// (Title→Action+EntityName, DetailContentChanges→Changes, UserId, INS_DTTM→CreatedAt).
/// Ghi tự động ở tầng ghi (SaveChanges interceptor): ai (UserId), làm gì (Action),
/// trên entity nào (EntityName/EntityId), thay đổi ra sao (Changes JSON).
/// Append-only: chỉ tạo, không sửa/xoá.
/// </summary>
public sealed class ActivityLog : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;      // Insert | Update | Delete
    public string EntityName { get; set; } = string.Empty;  // tên type entity nghiệp vụ
    public string EntityId { get; set; } = string.Empty;    // khoá của bản ghi bị tác động
    public string? Changes { get; set; }                    // JSON prop→{old,new} (Update) hoặc giá trị mới (Insert)
}
