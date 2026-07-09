using TourKit.Shared.Enums;

namespace TourKit.Application.Marketing.Dtos;

public sealed record CampaignDto(
    Guid Id, string Name, MarketingChannel Channel, string? Subject, string Body, int Status);

public sealed record CreateCampaignDto(string Name, MarketingChannel Channel, string? Subject, string Body);

public sealed record UpdateCampaignDto(
    string Name, MarketingChannel Channel, string? Subject, string Body, int Status);

public sealed record SendCampaignDto(string[] Recipients);

public sealed record SendResultDto(int Sent);

public sealed record SendLogDto(Guid Id, string Recipient, int Status, DateTimeOffset SentAt);
