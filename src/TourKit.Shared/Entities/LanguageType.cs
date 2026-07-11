namespace TourKit.Shared.Entities;

/// <summary>
/// Danh mục ngôn ngữ HDV (legacy <c>LanguagesType</c>): chuẩn hoá ngôn ngữ hướng dẫn (Anh/Trung/Nhật/Hàn…)
/// cho quản lý HDV. Thuần catalog, <see cref="Name"/> duy nhất theo tenant.
/// </summary>
public sealed class LanguageType : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;   // tên ngôn ngữ
    public string? Code { get; set; }                  // mã ISO tuỳ chọn (vd "en", "zh")
    public int SortOrder { get; set; }
    public int Status { get; set; }
}
