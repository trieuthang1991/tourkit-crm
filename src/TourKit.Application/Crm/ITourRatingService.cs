using TourKit.Application.Common;
using TourKit.Application.Crm.Dtos;

namespace TourKit.Application.Crm;

public interface ITourRatingService
{
    Task<PagedResult<TourRatingDto>> ListAsync(int page, int size);
    Task<TourRatingDto> GetAsync(Guid id);
    Task<TourRatingDto> CreateAsync(CreateTourRatingDto dto);
    Task UpdateAsync(Guid id, UpdateTourRatingDto dto);
    Task DeleteAsync(Guid id);
}
