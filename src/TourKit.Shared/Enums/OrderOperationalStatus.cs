namespace TourKit.Shared.Enums;

/// <summary>
/// Tình trạng vận hành tour (legacy StatusTour) — vòng đời điều hành, khác OrderStatus (nháp/chốt/huỷ).
/// Dùng cho bộ lọc "Tình trạng" và thẻ thống kê vận hành trên màn Tất cả Tour/LKH.
/// </summary>
public enum OrderOperationalStatus
{
    Upcoming = 1,          // Sắp chạy
    Running = 2,           // Đang chạy
    PendingSettlement = 3, // Chưa quyết toán
    Settled = 4,           // Đã quyết toán
    Done = 5,              // Xong (hoàn thành)
    Cancelled = 6,         // Hủy
    CancelledNoShow = 7,   // Hủy không đi
}
