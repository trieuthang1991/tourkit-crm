namespace TourKit.Application.Crm.Dtos;

/// <summary>Chiến dịch chia số Sale — 1 dòng: chiến dịch + tiến độ chăm sóc + tỷ lệ chốt của lead trong chiến dịch.</summary>
public sealed record LeadCampaignDto(
    Guid Id, string Name, Guid? CreatedByUserId, string? CreatedByName, DateTimeOffset CreatedAt, int Status,
    int TotalLeads, int CaredCount, int ClosedCount, decimal Progress, decimal CloseRate);

/// <summary>Bộ lọc chiến dịch chia số: tên chiến dịch · người tạo.</summary>
public sealed record LeadCampaignListFilter(string? Q = null, Guid? CreatedByUserId = null);

/// <summary>Thẻ đầu màn: tổng chiến dịch · tổng leads · tỷ lệ chốt TB · hoàn thành.</summary>
public sealed record LeadCampaignStatsDto(int TotalCampaigns, int TotalLeads, decimal AvgCloseRate, int Completed);

/// <summary>Tạo chiến dịch chia số mới.</summary>
public sealed record CreateLeadCampaignDto(string Name, string? Note);
