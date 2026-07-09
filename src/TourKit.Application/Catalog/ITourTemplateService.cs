using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Common;

namespace TourKit.Application.Catalog;

public interface ITourTemplateService
{
    Task<PagedResult<TourTemplateDto>> ListAsync(int page, int size);
    Task<TourTemplateDto> GetAsync(Guid id);
    Task<TourTemplateDto> CreateAsync(CreateTourTemplateDto dto);
    Task UpdateAsync(Guid id, UpdateTourTemplateDto dto);
    Task DeleteAsync(Guid id);
    Task<IReadOnlyList<ItineraryDayDto>> GetItineraryAsync(Guid id);
    Task ReplaceItineraryAsync(Guid id, IReadOnlyList<ItineraryDayDto> days);
    Task<IReadOnlyList<PriceScenarioDto>> GetPriceScenariosAsync(Guid id);
    Task ReplacePriceScenariosAsync(Guid id, IReadOnlyList<PriceScenarioDto> scenarios);
}
