namespace TourKit.Shared.Entities;

/// <summary>
/// Phân xe cho chuyến (điều hành, song song <see cref="TourGuideAssignment"/>): gắn một <see cref="Vehicle"/>
/// vào một chuyến (<see cref="TourDeparture"/>) kèm tài xế + giờ đón/trả. Một chuyến có thể có nhiều xe.
/// Status bám legacy State: 1=Created, 2=Active, 4=Delete.
/// </summary>
public sealed class VehicleAssignment : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid TourDepartureId { get; set; }        // chuyến
    public Guid VehicleId { get; set; }              // xe (danh mục Vehicle)
    public string? DriverName { get; set; }          // tài xế
    public string? DriverPhone { get; set; }         // SĐT tài xế
    public DateTimeOffset? TimeGo { get; set; }       // giờ đón
    public DateTimeOffset? TimeCome { get; set; }     // giờ trả (>= TimeGo)
    public string? Note { get; set; }
    public int Status { get; set; } = 1;              // legacy State: 1=Created, 2=Active, 4=Delete
}
