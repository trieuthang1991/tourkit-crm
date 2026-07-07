using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Entities;

public sealed class Role : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
}
