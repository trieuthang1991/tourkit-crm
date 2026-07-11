using TourKit.Application.Catalog.Dtos;

namespace TourKit.Application.Catalog;

public interface ILanguageTypeService
{
    Task<IReadOnlyList<LanguageTypeDto>> ListAsync();
    Task<LanguageTypeDto> CreateAsync(CreateLanguageTypeDto dto);
    Task UpdateAsync(Guid id, UpdateLanguageTypeDto dto);
    Task DeleteAsync(Guid id);
}
