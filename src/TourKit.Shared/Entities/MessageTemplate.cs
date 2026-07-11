using TourKit.Shared.Enums;

namespace TourKit.Shared.Entities;

/// <summary>
/// Mẫu tin nhắn tái sử dụng (legacy <c>Email_Sample</c>/<c>Marketing_Template</c>): soạn sẵn nội dung
/// email/SMS/Zalo để tạo nhanh chiến dịch. Tự chứa, không phụ thuộc dịch vụ ngoài (chỉ là nội dung mẫu).
/// </summary>
public sealed class MessageTemplate : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public MarketingChannel Channel { get; set; } = MarketingChannel.Email;
    public string? Subject { get; set; }        // chỉ dùng cho Email
    public string Body { get; set; } = string.Empty;
}
