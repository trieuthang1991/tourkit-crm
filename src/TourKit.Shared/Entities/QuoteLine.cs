namespace TourKit.Shared.Entities;

/// <summary>Một dòng của <see cref="Quote"/>: mô tả hạng mục, số lượng × đơn giá. Amount = Quantity × UnitPrice (tính khi đọc).</summary>
public sealed class QuoteLine : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid QuoteId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }                // giá BÁN đơn vị (giữ nghĩa cũ)

    // --- Dự trù giá (spec 2026-07-11): giá vốn + %LN theo dòng (legacy percent_loi_nhuan_khach) ---
    public int ServiceType { get; set; }                  // QuoteLineServiceType (Other=0 khớp dòng cũ)
    public int Scope { get; set; }                        // QuoteLineScope (PerGroup=0 khớp dòng cũ)
    public Guid? ProviderServiceId { get; set; }          // tham chiếu bảng giá NCC (tuỳ chọn)
    public decimal UnitCost { get; set; }                 // giá vốn đơn vị; 0 = báo giá nhanh gõ tay
    public decimal MarginPercent { get; set; }            // %LN dòng → UnitPrice = UnitCost×(1+%/100)
}
