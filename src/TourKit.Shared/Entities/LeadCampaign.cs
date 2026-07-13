namespace TourKit.Shared.Entities;

/// <summary>
/// Chiến dịch chia số Sale (legacy "Chia số Sale"): gom một tập lead/data khách và phân bổ cho sale,
/// theo dõi tiến độ chăm sóc + tỷ lệ chốt. Lead thuộc chiến dịch qua <see cref="Lead.CampaignId"/>.
/// </summary>
public sealed class LeadCampaign : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? CreatedByUserId { get; set; }   // Người tạo chiến dịch
    public int Status { get; set; }              // 0 đang chạy, 1 hoàn thành
    public string? Note { get; set; }
}
