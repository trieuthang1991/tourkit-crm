using TourKit.Application.Common;
using TourKit.Application.Providers.Dtos;

namespace TourKit.Application.Providers;

public interface IProviderService
{
    Task<PagedResult<ProviderDto>> ListAsync(int page, int size, ProviderListFilter? filter = null);
    Task<ProviderStatsDto> GetStatsAsync();
    Task<ProviderDto> GetAsync(Guid id);

    /// <summary>
    /// NỐI THÊM dòng bảng giá cho một NCC — dùng cho nhập từ tệp.
    ///
    /// Khác <see cref="UpdateWithServicesAsync"/> ở chỗ đó thay TOÀN BỘ danh sách (dòng không gửi
    /// lên = đã xoá). Nhập tệp mà dùng nhầm đường kia là xoá sạch bảng giá đang có, chỉ còn lại
    /// những dòng trong tệp — mất dữ liệu mà không có gì báo.
    /// </summary>
    Task<int> ThemDichVuAsync(Guid providerId, IReadOnlyList<ProviderServiceLineDto> lines);
    Task<ProviderDto> CreateAsync(CreateProviderDto dto);
    Task UpdateAsync(Guid id, UpdateProviderDto dto);

    /// <summary>
    /// Sửa nhà cung cấp CÙNG bảng dịch vụ của nó, trong MỘT lần ghi.
    ///
    /// Bám hệ cũ (<c>uspInsertHotel</c>): thông tin NCC và danh sách sản phẩm/dịch vụ nằm chung một
    /// form và ghi chung một transaction — hỏng ở dòng dịch vụ thứ ba thì không có gì được ghi.
    /// Lưu từng dòng riêng lẻ sẽ để lại trạng thái nửa vời khi người dùng đóng trình duyệt giữa chừng.
    ///
    /// Dòng có sẵn mà không nằm trong <paramref name="services"/> sẽ bị XOÁ (xoá mềm).
    /// </summary>
    Task UpdateWithServicesAsync(Guid id, UpdateProviderDto dto, IReadOnlyList<ProviderServiceLineDto> services);
    Task DeleteAsync(Guid id);
}
