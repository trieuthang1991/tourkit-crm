namespace TourKit.Shared.Entities;

/// <summary>
/// Quỹ phòng / allotment (legacy "QUỸ PHÒNG KHÁCH SẠN" /room-fund/0) — MỖI dòng = 1 ô lịch:
/// (NCC × dịch vụ/loại phòng × ngày) với tồn (Quota/Booked) + giá NET theo loại ngày (DayType).
/// Grid vận hành: hàng = NCC+Dịch vụ, cột = ngày; ô tô màu theo DayType (thường/cuối tuần/lễ tết/cao điểm).
/// ID tham chiếu NCC lưu STRING để migrate dữ liệu legacy (pattern entity-extend-json-string).
/// </summary>
public sealed class RoomAllotment : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string ProviderRef { get; set; } = string.Empty;   // NCC (khách sạn/resort) — string ref
    public string ServiceName { get; set; } = string.Empty;   // Dịch vụ / loại phòng (vd "Deluxe Twin")
    public string? ProjectName { get; set; }                  // Tên dự án (lọc)
    public string? Province { get; set; }                     // Tỉnh thành (lọc theo địa bàn)
    public string? Market { get; set; }                       // Thị trường (lọc)
    public DateTimeOffset Date { get; set; }                  // Ngày áp dụng (cột lịch)
    public int DayType { get; set; }                          // 0 thường · 1 cuối tuần · 2 lễ tết · 3 cao điểm
    public int Quota { get; set; }                            // Tổng tồn (số phòng nhận giữ)
    public int Booked { get; set; }                           // Đã đặt (còn lại = Quota − Booked)
    public decimal Price { get; set; }                        // Giá NET/đêm theo ngày
    public int? Rating { get; set; }                          // Hạng sao NCC (lọc)
    public string? Note { get; set; }
}
