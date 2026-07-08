using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Entities;

/// <summary>Giá theo cỡ đoàn (bậc thang số khách) — grounded hệ cũ bảng PriceScenario.</summary>
public sealed class PriceScenario : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid TourTemplateId { get; set; }
    public int FromQty { get; set; }
    public int ToQty { get; set; }
    public decimal UnitPrice { get; set; }
    public int Status { get; set; }
}
