namespace TourKit.Shared.Entities;

/// <summary>
/// Danh mục loại khách (legacy <c>customer_type</c>): cho ý nghĩa cho <see cref="Customer.CustomerType"/> (int)
/// và <see cref="CustomerCommissionRule.CustomerType"/> — cả hai tham chiếu <see cref="Code"/>.
/// Thuần catalog (không đổi schema Customer). <see cref="Code"/> duy nhất theo tenant.
/// </summary>
public sealed class CustomerType : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public int Code { get; set; }                      // legacy customer_type.id — khớp Customer.CustomerType
    public string Name { get; set; } = string.Empty;   // ct_name
    public int SortOrder { get; set; }                 // NumberSort
    public int Status { get; set; }
}
