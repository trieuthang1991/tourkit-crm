namespace TourKit.Shared.Enums;

/// <summary>
/// Loại ngày cho quỹ phòng/allotment (bám hệ cũ — tô màu ô lịch giá): quyết định mức giá NET theo ngày.
/// Ngày thường (xanh dương) · Cuối tuần (xanh lá) · Lễ tết (đỏ) · Cao điểm (vàng).
/// </summary>
public enum RoomDayType
{
    Normal = 0,   // Ngày thường
    Weekend = 1,  // Cuối tuần
    Holiday = 2,  // Lễ tết
    Peak = 3,     // Cao điểm
}
