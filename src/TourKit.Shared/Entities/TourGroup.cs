namespace TourKit.Shared.Entities;

/// <summary>
/// Nhóm tour (legacy Nhóm): gom tour/đơn theo nhóm để lọc trên màn vận hành. Thuần catalog,
/// <see cref="Name"/> duy nhất theo tenant.
/// </summary>
public sealed class TourGroup : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int SortOrder { get; set; }
    public int Status { get; set; }
}
