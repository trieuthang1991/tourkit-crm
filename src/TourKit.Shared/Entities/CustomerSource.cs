namespace TourKit.Shared.Entities;

/// <summary>
/// Danh mục nguồn khách (legacy <c>customer_source</c>, cột <c>ct_source</c>): chuẩn hoá giá trị cho
/// <see cref="Customer.Source"/> (string) — Customer.Source lưu đúng <see cref="Name"/>. Thuần catalog.
/// </summary>
public sealed class CustomerSource : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;   // ct_source
    public int SortOrder { get; set; }
    public int Status { get; set; }
}
