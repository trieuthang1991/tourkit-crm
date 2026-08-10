using TourKit.Application.Booking.Dtos;
using TourKit.Application.Common;

namespace TourKit.Application.Booking;

public interface IDepartureService
{
    Task<PagedResult<DepartureDto>> ListAsync(int page, int size, DepartureListFilter? filter = null);
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
