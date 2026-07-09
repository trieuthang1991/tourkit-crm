using TourKit.Application.Catalog.Dtos;

namespace TourKit.Application.Catalog;

public interface IMarketTypeService
{
    Task<IReadOnlyList<MarketTypeDto>> ListAsync();
    Task<MarketTypeDto> CreateAsync(CreateMarketTypeDto dto);
    Task UpdateAsync(Guid id, UpdateMarketTypeDto dto);
    Task DeleteAsync(Guid id);
}
