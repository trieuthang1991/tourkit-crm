using TourKit.Application.Common;
using TourKit.Application.Marketing.Dtos;

namespace TourKit.Application.Marketing;

public interface ICampaignService
{
    Task<PagedResult<CampaignDto>> ListAsync(int page, int size, CampaignListFilter? filter = null);
    Task<CampaignStatsDto> GetStatsAsync();
    Task<CampaignDto> GetAsync(Guid id);
    Task<CampaignDto> CreateAsync(CreateCampaignDto dto);
    Task UpdateAsync(Guid id, UpdateCampaignDto dto);
    Task DeleteAsync(Guid id);
    Task<SendResultDto> SendAsync(Guid id, SendCampaignDto dto);
    Task<IReadOnlyList<SendLogDto>> ListLogsAsync(Guid id);
}
