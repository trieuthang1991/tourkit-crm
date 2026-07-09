
namespace TourKit.Shared.Entities;

/// <summary>Thị trường khách (cây cha-con) — grounded hệ cũ bảng MarketType.</summary>
public sealed class MarketType : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public int SortOrder { get; set; }
    public int Status { get; set; }
}
