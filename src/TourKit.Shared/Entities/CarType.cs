namespace TourKit.Shared.Entities;

/// <summary>
/// Danh mục loại xe (legacy <c>CarType</c>): cho ý nghĩa tên cho <see cref="Vehicle.SeatType"/> (int).
/// <see cref="Code"/> = số ghế (4/7/16/29/45…), duy nhất theo tenant; khớp Vehicle.SeatType. Thuần catalog.
/// </summary>
public sealed class CarType : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public int Code { get; set; }                      // số ghế — khớp Vehicle.SeatType
    public string Name { get; set; } = string.Empty;   // vd "Xe 45 chỗ"
    public int SortOrder { get; set; }
    public int Status { get; set; }
}
