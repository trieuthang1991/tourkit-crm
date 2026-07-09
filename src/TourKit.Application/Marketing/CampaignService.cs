using FluentValidation;
using TourKit.Application.Common;
using TourKit.Application.Marketing.Dtos;
using TourKit.Shared.Entities;

namespace TourKit.Application.Marketing;

/// <summary>Quản lý chiến dịch marketing (Email/SMS/Zalo) + ghi log gửi.
/// Gửi thật (Email/SMS/Zalo provider) nằm ngoài phạm vi — chỉ ghi log; follow-up.</summary>
public sealed class CampaignService(
    IRepository<MarketingCampaign> repo,
    IRepository<MarketingSendLog> logRepo,
    IValidator<CreateCampaignDto> createValidator,
    IValidator<UpdateCampaignDto> updateValidator) : ICampaignService
{
    public async Task<PagedResult<CampaignDto>> ListAsync(int page, int size)
    {
        var (items, total) = await repo.PageAsync(page, size);
        var dtos = items.Select(Map).ToList();
        return new PagedResult<CampaignDto>(dtos, total, page, size);
    }

    public async Task<CampaignDto> GetAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        return Map(entity);
    }

    public async Task<CampaignDto> CreateAsync(CreateCampaignDto dto)
    {
        await Validate(createValidator, dto);

        var entity = new MarketingCampaign
        {
            Name = dto.Name.Trim(),
            Channel = dto.Channel,
            Subject = dto.Subject,
            Body = dto.Body,
        };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        return Map(entity);
    }

    public async Task UpdateAsync(Guid id, UpdateCampaignDto dto)
    {
        await Validate(updateValidator, dto);

        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        entity.Name = dto.Name.Trim();
        entity.Channel = dto.Channel;
        entity.Subject = dto.Subject;
        entity.Body = dto.Body;
        entity.Status = dto.Status;
        repo.Update(entity);
        await repo.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id);
        if (entity is null)
        {
            throw new NotFoundException();
        }

        repo.Remove(entity);
        await repo.SaveChangesAsync();
    }

    public async Task<SendResultDto> SendAsync(Guid id, SendCampaignDto dto)
    {
        var campaign = await repo.GetByIdAsync(id);
        if (campaign is null)
        {
            throw new NotFoundException();
        }

        var now = DateTimeOffset.UtcNow;
        foreach (var recipient in dto.Recipients)
        {
            // Chưa tích hợp gửi thật (Email/SMS/Zalo provider) — chỉ ghi log; follow-up.
            await logRepo.AddAsync(new MarketingSendLog
            {
                CampaignId = id,
                Recipient = recipient,
                Status = 1, // sent-simulated
                SentAt = now,
            });
        }

        campaign.Status = 1; // đã gửi
        repo.Update(campaign);

        // Ghi log + cập nhật trạng thái chiến dịch dùng 2 repo khác nhau (cùng unit-of-work ở DB thật,
        // nhưng flush riêng từng repo để tương thích cả FakeRepository trong unit test).
        await logRepo.SaveChangesAsync();
        await repo.SaveChangesAsync();

        return new SendResultDto(dto.Recipients.Length);
    }

    public async Task<IReadOnlyList<SendLogDto>> ListLogsAsync(Guid id)
    {
        var logs = await logRepo.ListAsync(l => l.CampaignId == id);
        return logs.OrderByDescending(l => l.SentAt).Select(MapLog).ToList();
    }

    private static async Task Validate<T>(IValidator<T> validator, T dto)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }
    }

    private static CampaignDto Map(MarketingCampaign c) => new(
        c.Id, c.Name, c.Channel, c.Subject, c.Body, c.Status);

    private static SendLogDto MapLog(MarketingSendLog l) => new(l.Id, l.Recipient, l.Status, l.SentAt);
}
