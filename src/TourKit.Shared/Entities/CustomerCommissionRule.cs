namespace TourKit.Shared.Entities;

/// <summary>
/// Quy tắc hoa hồng theo LOẠI KHÁCH (legacy id_customer_type): 1 loại khách → 1 % hoa hồng.
/// Bổ sung chiều customer-type cho <see cref="CommissionRule"/> (vốn theo user). Khớp Customer.CustomerType.
/// </summary>
public sealed class CustomerCommissionRule : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public int CustomerType { get; set; }
    public decimal Percentage { get; set; }
    public int Status { get; set; }
}
