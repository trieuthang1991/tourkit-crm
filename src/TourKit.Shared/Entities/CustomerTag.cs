namespace TourKit.Shared.Entities;

/// <summary>
/// Danh mục nhãn khách (legacy <c>Tags</c>/<c>customer_tag</c>): chuẩn hoá + tô màu cho
/// <see cref="Customer.Tag"/> (string) — Customer.Tag lưu đúng <see cref="Name"/>. Thuần catalog.
/// </summary>
public sealed class CustomerTag : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;   // tag_name
    public string? Color { get; set; }                 // legacy Tags.color (hex/tên màu AntD)
    public int SortOrder { get; set; }
    public int Status { get; set; }
}
