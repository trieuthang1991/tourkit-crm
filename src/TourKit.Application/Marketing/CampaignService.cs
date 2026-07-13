using FluentValidation;
using TourKit.Application.Common;
using TourKit.Application.Marketing.Dtos;
using TourKit.Application.Notifications;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.Application.Marketing;

/// <summary>Quản lý chiến dịch marketing (Email/SMS/Zalo) + ghi log gửi. Cả 3 kênh gửi qua abstraction
/// (<see cref="IEmailSender"/>/<see cref="ISmsSender"/>/<see cref="IZaloSender"/>): dev ghi log, prod
/// dùng provider thật khi cấu hình. Bền per-recipient (1 địa chỉ lỗi không chặn cả chiến dịch).</summary>
public sealed class CampaignService(
    IRepository<MarketingCampaign> repo,
    IRepository<MarketingSendLog> logRepo,
    IEmailSender emailSender,
    ISmsSender smsSender,
    IZaloSender zaloSender,
    IValidator<CreateCampaignDto> createValidator,
    IValidator<UpdateCampaignDto> updateValidator) : ICampaignService
{
    // MarketingSendLog.Status: 1 = gửi thành công (hoặc mô phỏng cho kênh chưa có provider), 2 = lỗi.
    private const int StatusSent = 1;
    private const int StatusFailed = 2;

    public async Task<PagedResult<CampaignDto>> ListAsync(int page, int size, CampaignListFilter? filter = null)
    {
        var f = filter ?? new CampaignListFilter();
        var kw = string.IsNullOrWhiteSpace(f.Q) ? null : f.Q.Trim();

        var all = await repo.ListAsync(c =>
            (f.Channel == null || (int)c.Channel == f.Channel) &&
            (f.Status == null || c.Status == f.Status));

        var filtered = all
            .Where(c => kw == null || c.Name.Contains(kw, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(c => c.CreatedAt)
            .ToList();

        var pageItems = filtered.Skip((page - 1) * size).Take(size).Select(Map).ToList();
        return new PagedResult<CampaignDto>(pageItems, filtered.Count, page, size);
    }

    public async Task<CampaignStatsDto> GetStatsAsync()
    {
        var all = await repo.ListAsync();
        var messages = (await logRepo.ListAsync()).Count(l => l.Status == StatusSent);
        return new CampaignStatsDto(
            all.Count,
            all.Count(c => c.Status == 0),
            all.Count(c => c.Status == 1),
            messages);
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
            // Kênh Email: gửi thật qua IEmailSender (dev ghi log, prod SMTP). Một địa chỉ lỗi không
            // làm hỏng cả chiến dịch — ghi Status=Failed cho địa chỉ đó rồi tiếp tục.
            var status = campaign.Channel switch
            {
                MarketingChannel.Email => await TrySendAsync(() => emailSender.SendAsync(recipient, campaign.Subject ?? campaign.Name, campaign.Body)),
                MarketingChannel.Sms => await TrySendAsync(() => smsSender.SendAsync(recipient, campaign.Body)),
                MarketingChannel.Zalo => await TrySendAsync(() => zaloSender.SendAsync(recipient, campaign.Body)),
                _ => StatusSent,
            };

            await logRepo.AddAsync(new MarketingSendLog
            {
                CampaignId = id,
                Recipient = recipient,
                Status = status,
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

    /// <summary>Gửi 1 địa chỉ, bền: lỗi (provider từ chối…) → Status=Failed, không chặn địa chỉ khác.</summary>
    private static async Task<int> TrySendAsync(Func<Task> send)
    {
        try
        {
            await send();
            return StatusSent;
        }
#pragma warning disable CA1031 // Gửi campaign phải bền: 1 địa chỉ lỗi không được chặn các địa chỉ khác.
        catch (Exception)
#pragma warning restore CA1031
        {
            return StatusFailed;
        }
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
