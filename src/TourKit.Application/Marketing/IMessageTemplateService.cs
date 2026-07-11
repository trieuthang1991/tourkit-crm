using TourKit.Application.Marketing.Dtos;
using TourKit.Shared.Enums;

namespace TourKit.Application.Marketing;

/// <summary>Mẫu tin nhắn tái sử dụng (legacy Email_Sample/Marketing_Template) — CRUD, lọc theo kênh.</summary>
public interface IMessageTemplateService
{
    Task<IReadOnlyList<MessageTemplateDto>> ListAsync(MarketingChannel? channel);
    Task<MessageTemplateDto> CreateAsync(CreateMessageTemplateDto dto);
    Task UpdateAsync(Guid id, UpdateMessageTemplateDto dto);
    Task DeleteAsync(Guid id);
}
