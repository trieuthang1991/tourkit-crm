namespace TourKit.Shared.Entities;

/// <summary>
/// Vé máy bay đoàn (legacy "Quản lý Vé Đoàn"): quỹ vé theo PNR — số lượng, đã dùng, chi/thanh toán/bảo lưu,
/// gán vào tour/đơn. Hành trình các chặng lưu jsonb (ItineraryJson). ID tham chiếu (Thị trường/NCC/Đơn) lưu
/// STRING để migrate được dữ liệu legacy (ID cũ không phải GUID) — theo pattern entity-extend-json-string.
/// </summary>
public sealed class FlightTicket : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Pnr { get; set; } = string.Empty;    // Mã PNR (tìm kiếm)
    public string? MarketRef { get; set; }             // Thị trường (string ref)
    public string? ProviderRef { get; set; }           // NCC vé (string ref)
    public string? TourType { get; set; }              // Loại hình (inbound/outbound/domestic)
    public int Days { get; set; }                      // Số ngày
    public DateTimeOffset? DepartureDate { get; set; } // Ngày đi
    public int Quantity { get; set; }                  // Số lượng vé
    public int UsedQuantity { get; set; }              // Đã sử dụng
    public string? OrderRef { get; set; }              // Gán tour/đơn (string ref; null = chưa gán)
    public decimal TotalCost { get; set; }             // Tổng chi
    public decimal PaidAmount { get; set; }            // Đã thanh toán
    public decimal ReservedAmount { get; set; }        // Tiền bảo lưu
    public int Status { get; set; }
    public string? Note { get; set; }
    public string? ItineraryJson { get; set; }         // Hành trình các chặng (jsonb)
}
