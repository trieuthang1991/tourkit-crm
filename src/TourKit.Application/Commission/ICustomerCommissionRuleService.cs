using TourKit.Application.Commission.Dtos;
using TourKit.Application.Common;

namespace TourKit.Application.Commission;

public interface ICustomerCommissionRuleService
{
    Task<PagedResult<CustomerCommissionRuleDto>> ListAsync(int page, int size);
    Task<CustomerCommissionRuleDto> CreateAsync(CreateCustomerCommissionRuleDto dto);
    Task UpdateAsync(Guid id, UpdateCustomerCommissionRuleDto dto);
    Task DeleteAsync(Guid id);
}
