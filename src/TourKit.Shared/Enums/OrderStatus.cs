namespace TourKit.Shared.Enums;

public enum OrderStatus
{
    Draft = 1,
    Confirmed = 2,
    Cancelled = 3,
    Closed = 4, // Tất toán/chốt đơn: đã thu đủ + quyết hoa hồng → đóng đơn (legacy ChotDon). Khoá sửa.
}
