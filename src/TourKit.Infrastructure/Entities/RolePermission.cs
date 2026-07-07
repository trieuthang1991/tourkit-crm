using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Entities;

public sealed class RolePermission : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
}
