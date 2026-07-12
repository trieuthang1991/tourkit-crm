namespace TourKit.Shared.Entities;

/// <summary>
/// Chi nhánh (legacy ChiNhanh): cơ cấu tổ chức theo chi nhánh, dùng để lọc/báo cáo trên các màn giao dịch.
/// Thuần catalog, <see cref="Name"/> duy nhất theo tenant.
/// </summary>
public sealed class Branch : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int SortOrder { get; set; }
    public int Status { get; set; }
}
