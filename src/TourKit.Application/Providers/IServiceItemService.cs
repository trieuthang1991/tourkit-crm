using TourKit.Application.Common;
using TourKit.Application.Providers.Dtos;

namespace TourKit.Application.Providers;

public interface IServiceItemService
{
    Task<PagedResult<ServiceItemDto>> ListAsync(int page, int size);
    Task<ServiceItemDto> GetAsync(Guid id);
    Task<ServiceItemDto> CreateAsync(CreateServiceItemDto dto);
    Task UpdateAsync(Guid id, UpdateServiceItemDto dto);
    Task DeleteAsync(Guid id);
}
