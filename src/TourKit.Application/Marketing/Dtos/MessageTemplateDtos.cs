using TourKit.Shared.Enums;

namespace TourKit.Application.Marketing.Dtos;

public sealed record MessageTemplateDto(
    Guid Id, string Name, MarketingChannel Channel, string? Subject, string Body);

public sealed record CreateMessageTemplateDto(string Name, MarketingChannel Channel, string? Subject, string Body);

public sealed record UpdateMessageTemplateDto(string Name, MarketingChannel Channel, string? Subject, string Body);
