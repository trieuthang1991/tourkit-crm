using TourKit.Application.Common;
using TourKit.Application.Crm.Dtos;

namespace TourKit.Application.Crm;

public interface ILeadCampaignService
{
    Task<PagedResult<LeadCampaignDto>> ListAsync(int page, int size, LeadCampaignListFilter? filter = null);
    Task<LeadCampaignStatsDto> GetStatsAsync();
    Task<LeadCampaignDto> CreateAsync(CreateLeadCampaignDto dto);

    /// <summary>Đổi nhanh trạng thái Đang chạy(0)/Hoàn thành(1) từ menu trên dòng lưới.</summary>
    Task SetStatusAsync(Guid id, int status);
}
