using TourKit.Application.Common;
using TourKit.Application.Crm.Dtos;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.Application.Crm;

/// <summary>
/// Chiến dịch chia số Sale (legacy "Chia số Sale"): gom lead vào chiến dịch, theo dõi tiến độ chăm sóc
/// + tỷ lệ chốt. Số liệu mỗi chiến dịch tính từ Lead (CampaignId): tổng · đã chăm sóc (Status ≠ New) ·
/// đã chốt (Won) · tiến độ (đã xử lý/tổng) · tỷ lệ chốt (Won/tổng).
/// </summary>
public sealed class LeadCampaignService(
    IRepository<LeadCampaign> repo,
    IRepository<Lead> leadRepo,
    IRepository<User> userRepo) : ILeadCampaignService
{
    public async Task<PagedResult<LeadCampaignDto>> ListAsync(int page, int size, LeadCampaignListFilter? filter = null)
    {
        var f = filter ?? new LeadCampaignListFilter();
        var kw = string.IsNullOrWhiteSpace(f.Q) ? null : f.Q.Trim();

        var all = await repo.ListAsync(c => f.CreatedByUserId == null || c.CreatedByUserId == f.CreatedByUserId);
        var filtered = all
            .Where(c => kw == null || c.Name.Contains(kw, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(c => c.CreatedAt)
            .ToList();

        var pageItems = filtered.Skip((page - 1) * size).Take(size).ToList();

        var leadsByCampaign = (await leadRepo.ListAsync(l => l.CampaignId != null))
            .GroupBy(l => l.CampaignId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());
        var userNames = (await userRepo.ListAsync()).ToDictionary(u => u.Id, u => u.FullName);

        var dtos = pageItems.Select(c => Map(c, leadsByCampaign.GetValueOrDefault(c.Id, []), userNames)).ToList();
        return new PagedResult<LeadCampaignDto>(dtos, filtered.Count, page, size);
    }

    public async Task<LeadCampaignStatsDto> GetStatsAsync()
    {
        // Một câu GROUP BY cho mọi bậc trạng thái, thay vì nạp cả bảng hoặc bắn nhiều câu COUNT rời.
        var byCampaignStatus = await repo.CountByAsync(c => c.Status);
        var total = await leadRepo.CountAsync(l => l.CampaignId != null);
        var won = await leadRepo.CountAsync(l => l.CampaignId != null && l.Status == LeadStatus.Won);
        var avgClose = total == 0 ? 0m : Math.Round(won * 100m / total, 2);

        return new LeadCampaignStatsDto(
            byCampaignStatus.Values.Sum(), total, avgClose, byCampaignStatus.GetValueOrDefault(1));
    }

    public async Task<LeadCampaignDto> CreateAsync(CreateLeadCampaignDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new ValidationAppException("Tên chiến dịch bắt buộc.");
        }

        var entity = new LeadCampaign { Name = dto.Name.Trim(), Note = dto.Note, Status = 0 };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        var userNames = (await userRepo.ListAsync()).ToDictionary(u => u.Id, u => u.FullName);
        return Map(entity, [], userNames);
    }

    private static LeadCampaignDto Map(LeadCampaign c, List<Lead> leads, IReadOnlyDictionary<Guid, string> userNames)
    {
        var total = leads.Count;
        var cared = leads.Count(l => l.Status != LeadStatus.New);
        var closed = leads.Count(l => l.Status == LeadStatus.Won);
        var progress = total == 0 ? 0m : Math.Round(cared * 100m / total, 2);
        var closeRate = total == 0 ? 0m : Math.Round(closed * 100m / total, 2);
        return new LeadCampaignDto(
            c.Id, c.Name, c.CreatedByUserId,
            c.CreatedByUserId is { } uid ? userNames.GetValueOrDefault(uid) : null,
            c.CreatedAt, c.Status, total, cared, closed, progress, closeRate);
    }
}
