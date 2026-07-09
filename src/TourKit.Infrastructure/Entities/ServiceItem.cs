using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Entities;

/// <summary>Danh mục dịch vụ (legacy services): loại dịch vụ có thể mua của NCC (phòng, xe, vé, visa...).</summary>
public sealed class ServiceItem : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Category { get; set; }   // 1 Hotel,2 Vehicle,3 Restaurant,4 Guide,5 Air,6 Visa,7 Other (khớp ProviderType + Visa)
    public int Status { get; set; }
}
