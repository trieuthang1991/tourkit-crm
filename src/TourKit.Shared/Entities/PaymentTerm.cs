namespace TourKit.Shared.Entities;

/// <summary>
/// Điều khoản thanh toán NCC (legacy <c>ServicePaymentTerm</c>): mô tả lịch/điều kiện trả tiền cho NCC
/// (vd "Cọc 30%, còn lại trước khởi hành 7 ngày"). Gán vào <see cref="Provider.PaymentTermId"/>.
/// Thuần catalog, <see cref="Name"/> duy nhất theo tenant.
/// </summary>
public sealed class PaymentTerm : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public int Status { get; set; }
}
