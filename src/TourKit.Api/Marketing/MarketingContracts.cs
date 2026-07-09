using TourKit.Shared.Entities;

namespace TourKit.Api.Marketing;

public sealed record CreateCampaignRequest(string Name, MarketingChannel Channel, string? Subject, string Body);

public sealed record UpdateCampaignRequest(
    string Name, MarketingChannel Channel, string? Subject, string Body, int Status);

public sealed record CampaignResponse(
    Guid Id, string Name, MarketingChannel Channel, string? Subject, string Body, int Status);

public sealed record SendCampaignRequest(string[] Recipients);

public sealed record SendResultResponse(int Sent);

public sealed record SendLogResponse(Guid Id, string Recipient, int Status, DateTimeOffset SentAt);
