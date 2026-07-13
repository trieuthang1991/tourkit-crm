namespace TourKit.Shared.Entities;

/// <summary>
/// Vé máy bay lẻ (legacy "Quản lý Vé máy bay lẻ" /individual-airplane-ticket) — lưới vận hành vé cá nhân:
/// mỗi vé có PNR + khách + hành trình + P/L RIÊNG (thu/chi/lợi nhuận/công nợ) + hạn chi. KHÁC vé đoàn
/// (quỹ quota theo số lượng). ID tham chiếu (NCC/Đơn/NV) lưu STRING để migrate dữ liệu legacy
/// (theo pattern entity-extend-json-string). Trạng thái duyệt: 0 tạo mới · 1 đã duyệt · 2 không duyệt.
/// </summary>
public sealed class FlightTicketIndividual : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;   // Mã hệ thống (VMB_xxxx) — tìm kiếm
    public string? TicketCode { get; set; }            // Mã vé (số vé hãng)
    public string Pnr { get; set; } = string.Empty;    // PNR
    public string CustomerName { get; set; } = string.Empty; // Khách đi vé
    public string? OrderRef { get; set; }              // Đơn hàng liên kết (string ref; null = chưa gắn)
    public string? ProviderRef { get; set; }           // NCC vé / hãng (string ref)
    public int TripType { get; set; }                  // 0 một chiều · 1 khứ hồi
    public string? Route { get; set; }                 // Hành trình (vd "SGN-HAN")
    public DateTimeOffset? DepartDate { get; set; }    // Ngày đi (CI)
    public DateTimeOffset? ReturnDate { get; set; }    // Ngày về (CO — khứ hồi)

    // --- P/L riêng từng vé (bám footer hệ cũ) ---
    public decimal SellAmount { get; set; }            // Tổng thu (giá bán khách)
    public decimal ReceivedAmount { get; set; }        // Thực thu (đã thu của khách)
    public decimal TotalCost { get; set; }             // Tổng chi (giá vé phải trả NCC)
    public decimal PaidAmount { get; set; }            // Thực chi (đã trả NCC)
    public DateTimeOffset? PaymentDueDate { get; set; } // Hạn chi (để cảnh báo đến hạn 24h/quá hạn)

    public int Status { get; set; }                    // 0 tạo mới · 1 đã duyệt · 2 không duyệt
    public string? AssigneeRef { get; set; }           // NV phụ trách (string ref)
    public string? Note { get; set; }
}
