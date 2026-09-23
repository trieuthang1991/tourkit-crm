using TourKit.Application.Common;
using TourKit.Application.Crm.Dtos;

namespace TourKit.Application.Crm;

public interface ILeadCampaignService
{
    Task<PagedResult<LeadCampaignDto>> ListAsync(int page, int size, LeadCampaignListFilter? filter = null);
    Task<LeadCampaignStatsDto> GetStatsAsync();
    Task<LeadCampaignDto> CreateAsync(CreateLeadCampaignDto dto);

    Task<LeadCampaignDto> GetAsync(Guid id);

    Task UpdateAsync(Guid id, UpdateLeadCampaignDto dto);

    /// <summary>Đổi nhanh trạng thái Đang chạy(0)/Hoàn thành(1) từ menu trên dòng lưới.</summary>
    Task SetStatusAsync(Guid id, int status);

    /// <summary>
    /// Tra chiến dịch theo MÃ rồi chia số cho nhóm — cửa mà form thu lead đi vào.
    /// <c>null</c> khi mã không tra được.
    /// </summary>
    Task<ChiaSoKetQuaDto?> ChiaSoAsync(string? code);

    /// <summary>
    /// Cấu hình chia số theo mã — phần GẦN NHƯ KHÔNG ĐỔI, tách ra để tầng ngoài bọc cache.
    /// </summary>
    Task<CauHinhChiaSoDto?> TimCauHinhChiaSoAsync(string? code);
}
