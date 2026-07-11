
namespace TourKit.Shared.Entities;

public class User : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastLoginAt { get; set; }

    // Cơ cấu tổ chức (legacy PhongBan/Position) — nullable, không đổi hành vi auth/provisioning.
    public Guid? DepartmentId { get; set; }
    public Guid? PositionId { get; set; }
}
