using TourKit.Application.Commission.Dtos;
using TourKit.Application.Common;

namespace TourKit.Application.Commission;

public interface ICommissionRuleService
{
    Task<PagedResult<CommissionRuleDto>> ListAsync(int page, int size);
    Task<CommissionRuleDto> CreateAsync(CreateCommissionRuleDto dto);
    Task UpdateAsync(Guid id, UpdateCommissionRuleDto dto);
    Task DeleteAsync(Guid id);
}
