using TourKit.Application.Catalog.Dtos;

namespace TourKit.Application.Catalog;

public interface ISurchargeService
{
    Task<IReadOnlyList<SurchargeDto>> ListAsync();
    Task<SurchargeDto> CreateAsync(CreateSurchargeDto dto);
    Task UpdateAsync(Guid id, UpdateSurchargeDto dto);
    Task DeleteAsync(Guid id);
}
