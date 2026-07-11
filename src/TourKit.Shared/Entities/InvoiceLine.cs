namespace TourKit.Shared.Entities;

/// <summary>
/// Một dòng của <see cref="Invoice"/>: tiền hàng = Quantity × UnitPrice; tiền thuế = tiền hàng × VatRate%.
/// (giá trị tính khi đọc/ghi, không lưu dư thừa).
/// </summary>
public sealed class InvoiceLine : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid InvoiceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatRate { get; set; }   // % (vd 0, 5, 8, 10)
}
