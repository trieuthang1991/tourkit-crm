
namespace TourKit.Shared.Entities;

/// <summary>Log gửi cho từng người nhận của một chiến dịch. Không gọi provider thật — chỉ ghi nhận.</summary>
public sealed class MarketingSendLog : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid CampaignId { get; set; }
    public string Recipient { get; set; } = string.Empty;
    public int Status { get; set; }
    public DateTimeOffset SentAt { get; set; }
}
