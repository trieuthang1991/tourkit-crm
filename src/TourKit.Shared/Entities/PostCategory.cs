namespace TourKit.Shared.Entities;

/// <summary>Danh mục bài viết (legacy <c>CategoriesPost</c>): phân loại tin/bài. Slug duy nhất theo tenant.</summary>
public sealed class PostCategory : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int Status { get; set; }
}
