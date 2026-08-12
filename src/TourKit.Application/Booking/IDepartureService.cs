using TourKit.Application.Booking.Dtos;
using TourKit.Application.Common;

namespace TourKit.Application.Booking;

public interface IDepartureService
{
    Task<PagedResult<DepartureDto>> ListAsync(int page, int size, DepartureListFilter? filter = null);

    /// <summary>
    /// Gợi ý chuyến cho ô chọn gọi server.
    ///
    /// Tách riêng khỏi <see cref="ListAsync"/> chứ KHÔNG gọi lại nó: ListAsync nạp cả bảng chuyến về
    /// bộ nhớ rồi mới lọc, và với mỗi chuyến ở trang hiện tại còn nạp thêm đơn hàng cùng chỗ ngồi để
    /// tính Giữ/Bán/Còn. Dùng nó cho ô chọn nghĩa là mỗi lần người dùng gõ một ký tự lại quét cả bảng
    /// — đắt hơn cả cách nạp sẵn mà ta đang thay thế. Bản này đẩy lọc/sắp/cắt xuống SQL.
    /// </summary>
    Task<IReadOnlyList<DepartureLookupDto>> LookupAsync(string? q, int take = 20);

    /// <summary>
    /// Lấy nhãn của một TẬP chuyến theo id, bằng MỘT câu truy vấn.
    ///
    /// Dùng khi màn khác cần hiện tên chuyến cho cả một trang dữ liệu (vd cột "Chuyến" ở lưới Cơ
    /// hội). Gọi <see cref="GetAsync"/> trong vòng lặp thì mỗi dòng một câu SQL — 20 dòng là 20 câu,
    /// và không ai nhận ra vì màn vẫn chạy, chỉ chậm dần theo kích thước trang.
    /// </summary>
    Task<IReadOnlyList<DepartureLookupDto>> ByIdsAsync(IReadOnlyList<Guid> ids);

    Task<DepartureStatsDto> GetStatsAsync();
    Task<DepartureFilterOptionsDto> GetFilterOptionsAsync();
    Task<DepartureDto> GetAsync(Guid id);
    Task<DepartureDto> CreateAsync(CreateDepartureDto dto);

    /// <summary>
    /// Sửa một chuyến đang mở.
    ///
    /// Chuyến ĐÃ ĐÓNG thì từ chối: đóng chuyến là khoá đặt chỗ, và mọi thứ sau đó (chốt hoa hồng,
    /// đối soát) đều dựa trên số liệu tại thời điểm đóng. Sửa ngược lại là làm sai những gì đã chốt.
    /// </summary>
    Task<DepartureDto> UpdateAsync(Guid id, UpdateDepartureDto dto);
    Task<BatchCreateResultDto> BatchCreateAsync(BatchCreateDeparturesDto dto);
    Task<DepartureDto> CloseAsync(Guid id);
    Task<DepartureDto> CloseCommissionAsync(Guid id);
    Task<DepartureDto> ReopenCommissionAsync(Guid id);
}
