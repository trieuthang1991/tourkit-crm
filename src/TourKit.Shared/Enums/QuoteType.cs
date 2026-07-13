namespace TourKit.Shared.Enums;

/// <summary>
/// Loại báo giá — bám 7 mục menu "Báo Giá" hệ cũ (staging):
/// Tính giá Tour · Tính giá Combo · Tour GIT/Combo · Landtour · Booking Phòng · Dịch vụ lẻ · Visa.
/// Cùng 1 khung calculator (dòng chi phí + công thức QuoteMath), khác loại để lọc/thống kê riêng.
/// </summary>
public enum QuoteType
{
    Tour = 0,
    Combo = 1,
    GitCombo = 2,
    Landtour = 3,
    RoomBooking = 4,
    Service = 5,
    Visa = 6,
}
