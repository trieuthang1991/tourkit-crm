namespace TourKit.Shared.Entities;

/// <summary>
/// Phòng ban (legacy <c>PhongBan</c>): cơ cấu tổ chức, gán vào <see cref="User.DepartmentId"/>.
/// Thuần catalog, <see cref="Name"/> duy nhất theo tenant. Phục vụ báo cáo/lọc theo phòng ban.
/// </summary>
public sealed class Department : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }                  // mã phòng ban tuỳ chọn
    public int SortOrder { get; set; }
    public int Status { get; set; }
}
