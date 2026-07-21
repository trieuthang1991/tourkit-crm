using TourKit.Application.Rooms.Dtos;
using TourKit.Shared.Entities;

namespace TourKit.Application.Rooms;

/// <summary>
/// Query gộp cho Quỹ phòng (convention §5: query phức tạp → interface repo riêng).
///
/// Vì sao phải tách: bảng quỹ phòng ~36.000 dòng. Bản cũ nạp TOÀN BỘ dòng khớp bộ lọc về bộ nhớ
/// rồi mới sắp xếp/cắt trang — và màn lưới lịch lại gọi nhiều lượt để quét theo lô, tức MỖI LƯỢT
/// quét lại cả bảng. Đưa lọc + sắp xếp + cắt trang + gộp thống kê xuống SQL thì mỗi lượt chỉ còn
/// đúng một truy vấn có LIMIT.
/// </summary>
public interface IRoomAllotmentQueries
{
    /// <summary>Lọc + sắp (NCC → dịch vụ → ngày) + cắt trang NGAY Ở SQL.</summary>
    Task<(IReadOnlyList<RoomAllotment> Items, int Total)> PageAsync(RoomAllotmentListFilter filter, int page, int size);

    /// <summary>Thẻ thống kê gộp bằng COUNT/SUM ở SQL — không kéo dòng nào về bộ nhớ.</summary>
    Task<RoomAllotmentStatsDto> StatsAsync(RoomAllotmentListFilter filter);
}
