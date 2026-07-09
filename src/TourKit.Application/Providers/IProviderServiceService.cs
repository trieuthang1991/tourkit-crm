using TourKit.Application.Common;
using TourKit.Application.Providers.Dtos;

namespace TourKit.Application.Providers;

public interface IProviderServiceService
{
    Task<PagedResult<ProviderServiceDto>> ListAsync(int page, int size, Guid? providerId);
    Task<ProviderServiceDto> GetAsync(Guid id);
    Task<ProviderServiceDto> CreateAsync(CreateProviderServiceDto dto);
    Task UpdateAsync(Guid id, UpdateProviderServiceDto dto);
    Task DeleteAsync(Guid id);
}
