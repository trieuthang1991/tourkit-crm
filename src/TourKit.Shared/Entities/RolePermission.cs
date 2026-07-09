
namespace TourKit.Shared.Entities;

public sealed class RolePermission : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
}
