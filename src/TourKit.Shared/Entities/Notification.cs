namespace TourKit.Shared.Entities;

/// <summary>
/// Thông báo in-app cho 1 user (legacy <c>Notification</c>/<c>NotificationOfEachUser</c>). Tự chứa,
/// không phụ thuộc dịch vụ ngoài. Sinh bởi hệ (vd giao việc) hoặc thủ công.
/// </summary>
public sealed class Notification : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }                   // người nhận
    public string Title { get; set; } = string.Empty;
    public string? Message { get; set; }
    public string? LinkUrl { get; set; }               // deep-link trong app (tuỳ chọn)
    public bool IsRead { get; set; }
}
