namespace TourKit.Shared.Entities;

/// <summary>
/// Bài viết/tin tức (legacy <c>Posts</c>): nội dung marketing (khuyến mãi, cẩm nang du lịch). Tự chứa,
/// không phụ thuộc dịch vụ ngoài. Slug duy nhất theo tenant. Status 0=nháp, 1=đã xuất bản (PublishedAt set).
/// </summary>
public sealed class Post : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string Body { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public int Status { get; set; }                    // 0 nháp, 1 đã xuất bản
    public DateTimeOffset? PublishedAt { get; set; }   // set khi chuyển sang xuất bản
    public Guid? AuthorUserId { get; set; }
    public int LikeCount { get; set; }                 // legacy Likes — số lượt thích (biên tập/curated)
}
