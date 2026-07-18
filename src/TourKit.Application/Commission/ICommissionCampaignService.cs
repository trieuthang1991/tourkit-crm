using TourKit.Application.Commission.Dtos;

namespace TourKit.Application.Commission;

/// <summary>
/// Chính sách hoa hồng bậc thang (legacy CommissionCompaign): CRUD header + nhân viên + bậc, có kiểm tra
/// chồng thời gian theo từng nhân viên, và tra cứu tỉ lệ áp dụng cho (nhân viên, ngày, lợi nhuận).
/// </summary>
public interface ICommissionCampaignService
{
    Task<IReadOnlyList<CommissionCampaignDto>> ListAsync();
    Task<CommissionCampaignDetailDto> GetAsync(Guid id);
    Task<CommissionCampaignDetailDto> CreateAsync(CreateCommissionCampaignDto dto);
    Task UpdateAsync(Guid id, UpdateCommissionCampaignDto dto);
    Task DeleteAsync(Guid id);

    /// <summary>Tra cứu tỉ lệ bậc thang áp dụng cho 1 nhân viên tại 1 ngày với 1 mức lợi nhuận.</summary>
    Task<TieredRateResultDto> ResolveRateAsync(Guid userId, DateTimeOffset date, decimal profit);
}
