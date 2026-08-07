namespace TourKit.Shared.Entities;

/// <summary>
/// Bình luận của NGƯỜI trên một bản ghi nghiệp vụ bất kỳ (cơ hội, khách hàng, đơn hàng…).
///
/// Đi cặp với <see cref="ActivityLog"/> và dùng CHUNG cặp khoá <c>(EntityName, EntityId)</c> để hai
/// dòng ghép được thành một dòng thời gian: ActivityLog trả lời "đã xảy ra chuyện gì" (hệ thống ghi,
/// append-only), EntityComment trả lời "vì sao" (người viết, sửa/xoá được). Thiếu vế sau thì nhìn vào
/// chỉ thấy cơ hội đứng yên mà không biết lý do.
/// </summary>
public sealed class EntityComment : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }

    /// <summary>Tên type entity nghiệp vụ — cùng quy ước với <see cref="ActivityLog.EntityName"/>.</summary>
    public string EntityName { get; set; } = string.Empty;

    /// <summary>Khoá của bản ghi được bình luận — cùng quy ước với <see cref="ActivityLog.EntityId"/>.</summary>
    public string EntityId { get; set; } = string.Empty;

    /// <summary>Người viết. Không nullable: bình luận luôn có tác giả, khác ActivityLog (hệ thống ghi được).</summary>
    public Guid UserId { get; set; }

    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Danh sách user được @nhắc, JSON mảng Guid. Để JSON thay vì bảng nối vì đây là trường dạng
    /// danh sách chỉ đọc kèm bình luận, không bao giờ truy vấn ngược "user này bị nhắc ở đâu".
    /// </summary>
    public string? MentionedUserIds { get; set; }
}
