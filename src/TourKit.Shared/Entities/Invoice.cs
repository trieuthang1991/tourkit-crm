namespace TourKit.Shared.Entities;

/// <summary>
/// Hoá đơn VAT (header) — chuẩn hoá đơn GTGT Việt Nam (legacy InvoiceBranch: xuất theo chi nhánh).
/// Gồm nhiều dòng; Subtotal = Σ tiền hàng, VatAmount = Σ tiền thuế, TotalAmount = Subtotal + VatAmount.
/// </summary>
public sealed class Invoice : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Series { get; set; } = string.Empty;   // ký hiệu (vd 1C25TAA)
    public string Number { get; set; } = string.Empty;   // số hoá đơn
    public DateTimeOffset InvoiceDate { get; set; }
    public Guid? OrderId { get; set; }                    // đơn liên quan (tuỳ chọn)
    public string BuyerName { get; set; } = string.Empty;
    public string? BuyerTaxCode { get; set; }             // MST người mua
    public string? BuyerAddress { get; set; }
    public decimal Subtotal { get; set; }                 // Σ tiền hàng (trước thuế)
    public decimal VatAmount { get; set; }                // Σ tiền thuế
    public decimal TotalAmount { get; set; }              // Subtotal + VatAmount
    public int Status { get; set; }                       // 0 nháp, 1 phát hành, 2 huỷ
    public string? Note { get; set; }
}
