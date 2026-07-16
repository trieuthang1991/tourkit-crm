namespace TourKit.Application.Rooms.Dtos;

/// <summary>
/// DTO 1 ô quỹ phòng trả client — Available (còn lại = Quota − Booked) derived + enrich tên NCC.
/// DayType: 0 thường · 1 cuối tuần · 2 lễ tết · 3 cao điểm (client tô màu ô theo giá trị này).
/// </summary>
public sealed record RoomAllotmentDto(
    Guid Id, string ProviderRef, string? ProviderName, string ServiceName,
    string? ProjectName, string? Province, string? Market,
    DateTimeOffset Date, int DayType, int Quota, int Booked, int Available,
    decimal Price, int? Rating, string? Note);

/// <summary>
/// Bộ lọc quỹ phòng (bám hệ cũ: tìm kiếm · tên dự án · tỉnh thành · thị trường · NCC · rating · khoảng ngày).
/// DateFrom/DateTo giới hạn cột lịch (grid nạp cả khoảng rồi gom hàng theo NCC+dịch vụ ở client).
/// </summary>
public sealed record RoomAllotmentListFilter(
    string? Q = null, string? ProjectName = null, string? Province = null, string? Market = null,
    string? ProviderRef = null, int? Rating = null,
    DateTimeOffset? DateFrom = null, DateTimeOffset? DateTo = null);

/// <summary>Thẻ tổng quỹ phòng: số ô · số NCC · tổng tồn · đã đặt · còn lại + đếm ô theo loại ngày.</summary>
public sealed record RoomAllotmentStatsDto(
    int Cells, int Providers, int TotalQuota, int TotalBooked, int TotalAvailable,
    int NormalDays, int WeekendDays, int HolidayDays, int PeakDays);

public sealed record CreateRoomAllotmentDto(
    string ProviderRef, string ServiceName, string? ProjectName, string? Province, string? Market,
    DateTimeOffset Date, int DayType, int Quota, int Booked, decimal Price, int? Rating, string? Note);

public sealed record UpdateRoomAllotmentDto(
    string ProviderRef, string ServiceName, string? ProjectName, string? Province, string? Market,
    DateTimeOffset Date, int DayType, int Quota, int Booked, decimal Price, int? Rating, string? Note);
