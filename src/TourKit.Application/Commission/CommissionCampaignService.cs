using FluentValidation;
using TourKit.Application.Commission.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Domain;
using TourKit.Shared.Entities;

namespace TourKit.Application.Commission;

/// <summary>
/// Chính sách hoa hồng bậc thang (legacy <c>ConfigCommission.UpdateCommissionCompaign</c>): một CHÍNH SÁCH
/// có tên + khoảng thời gian, áp cho một nhóm nhân viên, gồm nhiều bậc lợi nhuận. Lưu header + nhân viên +
/// bậc trong một SaveChanges (replace-children khi sửa, giống ApprovalProcess/Quote). Chặn chồng thời gian
/// giữa các chính sách ĐANG ÁP DỤNG có chung nhân viên.
/// </summary>
public sealed class CommissionCampaignService(
    IRepository<CommissionCampaign> repo,
    IRepository<CommissionCampaignUser> userRepo,
    IRepository<CommissionTier> tierRepo,
    IRepository<User> userAccountRepo,
    IValidator<CreateCommissionCampaignDto> createValidator,
    IValidator<UpdateCommissionCampaignDto> updateValidator) : ICommissionCampaignService
{
    public async Task<IReadOnlyList<CommissionCampaignDto>> ListAsync()
    {
        var campaigns = await repo.ListAsync();
        var users = await userRepo.ListAsync();
        var tiers = await tierRepo.ListAsync();
        return campaigns
            .OrderBy(c => c.Status)
            .ThenByDescending(c => c.StartDate)
            .Select(c => new CommissionCampaignDto(
                c.Id, c.Name, c.StartDate, c.EndDate, c.Status,
                users.Count(u => u.CommissionCampaignId == c.Id),
                tiers.Count(t => t.CommissionCampaignId == c.Id)))
            .ToList();
    }

    public async Task<CommissionCampaignDetailDto> GetAsync(Guid id)
    {
        var campaign = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        return await BuildDetailAsync(campaign);
    }

    public async Task<CommissionCampaignDetailDto> CreateAsync(CreateCommissionCampaignDto dto)
    {
        await Validate(createValidator, dto);
        var userIds = dto.UserIds.Distinct().ToList();
        await EnsureUsersExistAsync(userIds);
        await EnsureNoOverlapAsync(null, dto.StartDate, dto.EndDate, dto.Status, userIds);

        var campaign = new CommissionCampaign
        {
            Name = dto.Name.Trim(),
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = dto.Status,
        };
        await repo.AddAsync(campaign);

        foreach (var uid in userIds)
        {
            await userRepo.AddAsync(new CommissionCampaignUser { CommissionCampaignId = campaign.Id, UserId = uid });
        }

        foreach (var t in dto.Tiers)
        {
            await tierRepo.AddAsync(new CommissionTier
            {
                CommissionCampaignId = campaign.Id,
                StartAmount = t.StartAmount,
                EndAmount = t.EndAmount,
                Percentage = t.Percentage,
            });
        }

        // Repository<T> chia sẻ 1 AppDbContext ⇒ lần Save đầu flush cả header+nhân viên+bậc trong MỘT
        // transaction; các lần sau không có gì pending (no-op). Vẫn gọi từng repo để fake test (mỗi repo 1
        // list riêng) cũng persist đúng.
        await repo.SaveChangesAsync();
        await userRepo.SaveChangesAsync();
        await tierRepo.SaveChangesAsync();
        return await BuildDetailAsync(campaign);
    }

    public async Task UpdateAsync(Guid id, UpdateCommissionCampaignDto dto)
    {
        await Validate(updateValidator, dto);
        var campaign = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        var userIds = dto.UserIds.Distinct().ToList();
        await EnsureUsersExistAsync(userIds);
        await EnsureNoOverlapAsync(id, dto.StartDate, dto.EndDate, dto.Status, userIds);

        campaign.Name = dto.Name.Trim();
        campaign.StartDate = dto.StartDate;
        campaign.EndDate = dto.EndDate;
        campaign.Status = dto.Status;
        repo.Update(campaign);

        // Replace-children: xoá toàn bộ nhân viên + bậc cũ, thêm lại theo payload.
        foreach (var u in await userRepo.ListAsync(u => u.CommissionCampaignId == id))
        {
            userRepo.Remove(u);
        }

        foreach (var t in await tierRepo.ListAsync(t => t.CommissionCampaignId == id))
        {
            tierRepo.Remove(t);
        }

        foreach (var uid in userIds)
        {
            await userRepo.AddAsync(new CommissionCampaignUser { CommissionCampaignId = id, UserId = uid });
        }

        foreach (var t in dto.Tiers)
        {
            await tierRepo.AddAsync(new CommissionTier
            {
                CommissionCampaignId = id,
                StartAmount = t.StartAmount,
                EndAmount = t.EndAmount,
                Percentage = t.Percentage,
            });
        }

        await repo.SaveChangesAsync();
        await userRepo.SaveChangesAsync();
        await tierRepo.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var campaign = await repo.GetByIdAsync(id) ?? throw new NotFoundException();

        foreach (var u in await userRepo.ListAsync(u => u.CommissionCampaignId == id))
        {
            userRepo.Remove(u);
        }

        foreach (var t in await tierRepo.ListAsync(t => t.CommissionCampaignId == id))
        {
            tierRepo.Remove(t);
        }

        repo.Remove(campaign);
        await repo.SaveChangesAsync();
    }

    public async Task<TieredRateResultDto> ResolveRateAsync(Guid userId, DateTimeOffset date, decimal profit)
    {
        // Chính sách áp dụng: đang áp dụng (Status 0), ngày nằm trong [Start,End], có gán nhân viên này.
        var memberships = await userRepo.ListAsync(u => u.UserId == userId);
        var campaignIds = memberships.Select(m => m.CommissionCampaignId).ToHashSet();
        if (campaignIds.Count == 0)
        {
            return new TieredRateResultDto(false, null, null, 0m, 0m);
        }

        var candidates = (await repo.ListAsync(c => c.Status == 0))
            .Where(c => campaignIds.Contains(c.Id) && c.StartDate <= date && date <= c.EndDate)
            .OrderByDescending(c => c.StartDate)
            .ToList();

        var campaign = candidates.FirstOrDefault();
        if (campaign is null)
        {
            return new TieredRateResultDto(false, null, null, 0m, 0m);
        }

        var tiers = await tierRepo.ListAsync(t => t.CommissionCampaignId == campaign.Id);
        var rate = TieredCommissionMath.TieredRate(tiers, profit);
        var commission = TieredCommissionMath.TieredCommission(profit, tiers);
        return new TieredRateResultDto(true, campaign.Id, campaign.Name, rate, commission);
    }

    /// <summary>
    /// Chặn chồng thời gian: hai chính sách ĐANG ÁP DỤNG (Status 0) có CHUNG ít nhất một nhân viên thì
    /// [StartDate,EndDate] không được giao nhau (giao ⇔ s1 ≤ e2 ∧ s2 ≤ e1). Bỏ qua chính sách đang sửa.
    /// Chính sách ngừng (Status ≠ 0) không tham gia ràng buộc.
    /// </summary>
    private async Task EnsureNoOverlapAsync(
        Guid? selfId, DateTimeOffset start, DateTimeOffset end, int status, List<Guid> userIds)
    {
        if (status != 0 || userIds.Count == 0)
        {
            return;
        }

        var userSet = userIds.ToHashSet();
        var others = (await repo.ListAsync(c => c.Status == 0))
            .Where(c => (selfId == null || c.Id != selfId.Value) && start <= c.EndDate && c.StartDate <= end)
            .ToList();
        if (others.Count == 0)
        {
            return;
        }

        var otherIds = others.Select(c => c.Id).ToHashSet();
        var otherMembers = (await userRepo.ListAsync())
            .Where(u => otherIds.Contains(u.CommissionCampaignId) && userSet.Contains(u.UserId))
            .ToList();
        if (otherMembers.Count > 0)
        {
            throw new ValidationAppException(
                "Chính sách hoa hồng bị chồng khoảng thời gian với chính sách đang áp dụng khác trên cùng nhân viên.");
        }
    }

    private async Task EnsureUsersExistAsync(List<Guid> userIds)
    {
        foreach (var uid in userIds)
        {
            if (!await userAccountRepo.AnyAsync(u => u.Id == uid))
            {
                throw new ValidationAppException("Nhân viên áp dụng không tồn tại.");
            }
        }
    }

    private async Task<CommissionCampaignDetailDto> BuildDetailAsync(CommissionCampaign campaign)
    {
        var members = await userRepo.ListAsync(u => u.CommissionCampaignId == campaign.Id);
        var tiers = (await tierRepo.ListAsync(t => t.CommissionCampaignId == campaign.Id))
            .OrderBy(t => t.StartAmount)
            .ToList();

        var memberIds = members.Select(m => m.UserId).ToList();
        var names = (await userAccountRepo.ListAsync(u => memberIds.Contains(u.Id)))
            .ToDictionary(u => u.Id, u => string.IsNullOrWhiteSpace(u.FullName) ? u.Email : u.FullName);

        return new CommissionCampaignDetailDto(
            campaign.Id, campaign.Name, campaign.StartDate, campaign.EndDate, campaign.Status,
            memberIds,
            memberIds.Select(id => names.GetValueOrDefault(id, "?")).ToList(),
            tiers.Select(t => new CommissionTierDto(t.Id, t.StartAmount, t.EndAmount, t.Percentage)).ToList());
    }

    private static async Task Validate<T>(IValidator<T> validator, T dto)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }
    }
}
