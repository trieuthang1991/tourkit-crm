using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Entities;

public sealed class UserRole : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}
