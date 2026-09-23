using TourKit.Application.Common;
using TourKit.Application.Crm.Dtos;

namespace TourKit.Application.Crm;

public interface ITourRatingService
{
    Task<PagedResult<TourRatingDto>> ListAsync(int page, int size, string? q = null, int? stars = null, int? status = null,
        Guid? salesUserId = null, Guid? operatorUserId = null);
    Task<PagedResult<TourRatingByTourDto>> ListByTourAsync(int page, int size);
    Task<TourRatingStatsDto> GetStatsAsync();
    Task<TourRatingDto> GetAsync(Guid id);
    Task<TourRatingDto> CreateAsync(CreateTourRatingDto dto);
    Task UpdateAsync(Guid id, UpdateTourRatingDto dto);
    /// <summary>Đổi nhanh trạng thái kiểm duyệt: 0 Ẩn · 1 Hiển thị.</summary>
    Task SetStatusAsync(Guid id, int status);
    Task DeleteAsync(Guid id);
}
