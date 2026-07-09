
namespace TourKit.Shared.Entities;

public sealed class Role : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
}
