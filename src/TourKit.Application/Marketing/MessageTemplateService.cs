using TourKit.Application.Common;
using TourKit.Application.Marketing.Dtos;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.Application.Marketing;

/// <summary>
/// Mẫu tin nhắn tái sử dụng (legacy <c>Email_Sample</c>/<c>Marketing_Template</c>): soạn sẵn nội dung
/// email/SMS/Zalo để tạo nhanh chiến dịch. Thuần nội dung, không phụ thuộc provider ngoài.
/// </summary>
public sealed class MessageTemplateService(IRepository<MessageTemplate> repo) : IMessageTemplateService
{
    public async Task<IReadOnlyList<MessageTemplateDto>> ListAsync(MarketingChannel? channel)
    {
        var items = await repo.ListAsync(t => channel == null || t.Channel == channel);
        return items.OrderBy(t => t.Channel).ThenBy(t => t.Name).Select(Map).ToList();
    }

    public async Task<MessageTemplateDto> CreateAsync(CreateMessageTemplateDto dto)
    {
        Validate(dto.Name, dto.Body);
        var entity = new MessageTemplate
        {
            Name = dto.Name.Trim(),
            Channel = dto.Channel,
            Subject = dto.Subject?.Trim(),
            Body = dto.Body,
        };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();
        return Map(entity);
    }

    public async Task UpdateAsync(Guid id, UpdateMessageTemplateDto dto)
    {
        Validate(dto.Name, dto.Body);
        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        entity.Name = dto.Name.Trim();
        entity.Channel = dto.Channel;
        entity.Subject = dto.Subject?.Trim();
        entity.Body = dto.Body;
        repo.Update(entity);
        await repo.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        repo.Remove(entity);
        await repo.SaveChangesAsync();
    }

    private static void Validate(string name, string body)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationAppException("Tên mẫu không được trống.");
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ValidationAppException("Nội dung mẫu không được trống.");
        }
    }

    private static MessageTemplateDto Map(MessageTemplate t) => new(t.Id, t.Name, t.Channel, t.Subject, t.Body);
}
