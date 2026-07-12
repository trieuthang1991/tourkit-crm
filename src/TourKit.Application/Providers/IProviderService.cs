using TourKit.Application.Common;
using TourKit.Application.Providers.Dtos;

namespace TourKit.Application.Providers;

public interface IProviderService
{
    Task<PagedResult<ProviderDto>> ListAsync(int page, int size, ProviderListFilter? filter = null);
    Task<ProviderStatsDto> GetStatsAsync();
    Task<ProviderDto> GetAsync(Guid id);
    Task<ProviderDto> CreateAsync(CreateProviderDto dto);
    Task UpdateAsync(Guid id, UpdateProviderDto dto);
    Task DeleteAsync(Guid id);
}
