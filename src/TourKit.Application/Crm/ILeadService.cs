using TourKit.Application.Common;
using TourKit.Application.Crm.Dtos;

namespace TourKit.Application.Crm;

public interface ILeadService
{
    Task<PagedResult<LeadDto>> ListAsync(int page, int size);
    Task<LeadDto> GetAsync(Guid id);
    Task<LeadDto> CreateAsync(CreateLeadDto dto);
    Task UpdateAsync(Guid id, UpdateLeadDto dto);
    Task DeleteAsync(Guid id);
    Task<ConvertLeadResultDto> ConvertAsync(Guid id);
}
