namespace TourKit.Shared.Entities;

/// <summary>
/// Bình luận/đánh giá trên bài viết (legacy <c>CommentsPost</c>): do nhân viên nhập/duyệt (testimonial biên tập).
/// <see cref="IsApproved"/> = đã duyệt hiển thị. Cổng public tự gửi là follow-up; model + moderation ở đây.
/// </summary>
public sealed class PostComment : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid PostId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsApproved { get; set; } = true;
}
