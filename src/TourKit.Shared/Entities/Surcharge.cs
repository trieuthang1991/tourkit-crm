namespace TourKit.Shared.Entities;

/// <summary>
/// Danh mục loại phụ thu (legacy <c>ConfigSurcharge</c>): định nghĩa sẵn phụ thu (phòng đơn, cao điểm,
/// nhiên liệu…) để chọn khi thêm vào đơn. <see cref="CalcType"/> 0=số tiền cố định, 1=% trên giá gốc.
/// Thuần catalog, <see cref="Name"/> duy nhất theo tenant.
/// </summary>
public sealed class Surcharge : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CalcType { get; set; }                  // SurchargeCalcType (0 Fixed, 1 Percent)
    public decimal DefaultValue { get; set; }          // số tiền hoặc %, tuỳ CalcType
    public int SortOrder { get; set; }
    public int Status { get; set; }
}
