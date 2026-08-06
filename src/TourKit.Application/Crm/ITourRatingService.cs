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
    Task DeleteAsync(Guid id);
}
