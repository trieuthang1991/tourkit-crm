
namespace TourKit.Shared.Entities;

/// <summary>Chiến dịch marketing (Email/SMS/Zalo). Gửi thật nằm ngoài phạm vi — chỉ quản lý + ghi log.</summary>
public sealed class MarketingCampaign : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public MarketingChannel Channel { get; set; }
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public int Status { get; set; }
}
