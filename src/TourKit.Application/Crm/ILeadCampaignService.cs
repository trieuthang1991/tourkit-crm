using TourKit.Application.Common;
using TourKit.Application.Crm.Dtos;

namespace TourKit.Application.Crm;

public interface ILeadCampaignService
{
    Task<PagedResult<LeadCampaignDto>> ListAsync(int page, int size, LeadCampaignListFilter? filter = null);
    Task<LeadCampaignStatsDto> GetStatsAsync();
    Task<LeadCampaignDto> CreateAsync(CreateLeadCampaignDto dto);
}
