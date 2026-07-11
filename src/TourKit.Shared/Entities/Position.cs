namespace TourKit.Shared.Entities;

/// <summary>
/// Chức vụ (legacy <c>Position</c>): gán vào <see cref="User.PositionId"/>. Thuần catalog,
/// <see cref="Name"/> duy nhất theo tenant.
/// </summary>
public sealed class Position : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int Status { get; set; }
}
