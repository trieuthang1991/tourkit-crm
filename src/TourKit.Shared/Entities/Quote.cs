namespace TourKit.Shared.Entities;

/// <summary>
/// Báo giá (header) — grounded ở legacy (KojiCRM có template xuất báo giá tour/hotel). Chuẩn quotation:
/// gửi khách 1 bảng giá có hạn hiệu lực, gồm nhiều dòng; tổng tiền = Σ dòng. Chuyển thành đơn ở follow-up.
/// </summary>
public sealed class Quote : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }                 // KH đã có (tuỳ chọn)
    public string CustomerName { get; set; } = string.Empty; // tên KH (free text nếu chưa là customer)
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset? ValidUntil { get; set; }       // hạn hiệu lực
    public int Status { get; set; }                       // 0 nháp, 1 đã gửi, 2 chấp nhận, 3 từ chối
    public string? Note { get; set; }
    public decimal TotalAmount { get; set; }              // tổng giá bán — QuoteMath.Price, tính khi ghi

    // --- Dự trù giá (spec 2026-07-11): số khách theo hạng + % giá trẻ so với người lớn ---
    public int Adults { get; set; }
    public int Children { get; set; }
    public int Infants { get; set; }
    public decimal ChildPercent { get; set; } = 75m;      // legacy percent_price_tre_em
    public decimal InfantPercent { get; set; } = 50m;     // legacy percent_price_tre_nho
    public decimal TotalCost { get; set; }                // tổng giá vốn dự trù
    public decimal TotalProfit { get; set; }              // legacy loi_nhuan_du_kien = bán − vốn
    public Guid? ConvertedOrderId { get; set; }           // đơn sinh từ báo giá (legacy DuyetBooking) — idempotency
}
