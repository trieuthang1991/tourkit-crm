using TourKit.Application.Common;
using TourKit.Application.Customers.Dtos;

namespace TourKit.Application.Customers;

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> ListAsync(int page, int size, CustomerListFilter? filter = null);
    Task<CustomerStatsDto> GetStatsAsync();
    Task<CustomerFilterOptionsDto> GetFilterOptionsAsync();
    Task<CustomerFunnelDto> GetFunnelAsync();
    Task<CustomerDto> GetAsync(Guid id);
    Task<CustomerDto> CreateAsync(CreateCustomerDto dto);
    Task UpdateAsync(Guid id, UpdateCustomerDto dto);
    Task DeleteAsync(Guid id);

    /// <summary>Rà khách nghi trùng: gom theo SĐT/email đã chuẩn hoá, chỉ trả nhóm có ≥2 khách.</summary>
    Task<IReadOnlyList<DuplicateGroupDto>> FindDuplicatesAsync();
}
