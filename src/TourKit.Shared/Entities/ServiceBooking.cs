using TourKit.Shared.Enums;

namespace TourKit.Shared.Entities;

/// <summary>
/// Đặt dịch vụ lẻ (gộp legacy BookingHotel / AirPlaneTicket / Visa / BookingTicket) — một entity linh hoạt
/// phân biệt bằng <see cref="Type"/>. Gắn tuỳ chọn vào đơn (OrderId) + NCC (ProviderId). TotalAmount = SL × đơn giá.
/// </summary>
public sealed class ServiceBooking : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public ServiceBookingType Type { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? ProviderId { get; set; }
    public Guid? RoomClassId { get; set; }                    // hạng phòng (chỉ ý nghĩa khi Type=Hotel)
    public string Description { get; set; } = string.Empty;   // tên KS / chuyến bay / loại visa / vé...
    public DateTimeOffset? StartDate { get; set; }            // check-in / ngày bay / ngày cấp
    public DateTimeOffset? EndDate { get; set; }              // check-out / ngày về
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }                 // = Quantity × UnitPrice (tính khi ghi)
    public int Status { get; set; }
    public string? Note { get; set; }
}
