
using TourKit.Shared.Enums;

namespace TourKit.Shared.Entities;

/// <summary>Bảng gốc TPT — cột chung cho cả mẫu (Template) và chuyến (Departure).</summary>
public abstract class Tour : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public TourKind Kind { get; protected set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? TourType { get; set; }              // inbound/outbound/domestic... (thị trường)
    public int? Category { get; set; }                 // Loại sản phẩm tour: 0 FIT·1 GIT/Combo·2 LandTour·4 Dịch vụ·5 Visa (bám menu staging quản lý chuyến theo loại)
    public DateTimeOffset? DepartureDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public int TotalSlots { get; set; }
    public string? PickupPlace { get; set; }
    public string? DropoffPlace { get; set; }
    public string? TransportMode { get; set; }
    public Guid? ParentTourId { get; set; }            // Departure trỏ về Template nguồn (dùng ở Booking)
    public int Status { get; set; }
}
