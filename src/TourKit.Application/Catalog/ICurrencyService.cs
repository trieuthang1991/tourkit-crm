using TourKit.Application.Catalog.Dtos;

namespace TourKit.Application.Catalog;

public interface ICurrencyService
{
    Task<IReadOnlyList<CurrencyDto>> ListAsync();
    Task<CurrencyDto> CreateAsync(CreateCurrencyDto dto);
    Task UpdateAsync(Guid id, UpdateCurrencyDto dto);
    Task DeleteAsync(Guid id);
}
