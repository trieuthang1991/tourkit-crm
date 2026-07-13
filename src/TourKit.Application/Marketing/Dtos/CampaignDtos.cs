using TourKit.Shared.Enums;

namespace TourKit.Application.Marketing.Dtos;

public sealed record CampaignDto(
    Guid Id, string Name, MarketingChannel Channel, string? Subject, string Body, int Status);

/// <summary>Bộ lọc chiến dịch: kênh · trạng thái · từ khoá (tên). Status: 0 nháp, 1 đã gửi.</summary>
public sealed record CampaignListFilter(string? Q = null, int? Channel = null, int? Status = null);

/// <summary>Thẻ thống kê đầu màn Marketing: tổng chiến dịch · nháp · đã gửi · tổng tin nhắn đã gửi.</summary>
public sealed record CampaignStatsDto(int Total, int Draft, int Sent, int Messages);

public sealed record CreateCampaignDto(string Name, MarketingChannel Channel, string? Subject, string Body);

public sealed record UpdateCampaignDto(
    string Name, MarketingChannel Channel, string? Subject, string Body, int Status);

public sealed record SendCampaignDto(string[] Recipients);

public sealed record SendResultDto(int Sent);

public sealed record SendLogDto(Guid Id, string Recipient, int Status, DateTimeOffset SentAt);
