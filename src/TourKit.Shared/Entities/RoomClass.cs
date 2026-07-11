namespace TourKit.Shared.Entities;

/// <summary>Hạng phòng khách sạn (legacy <c>class_hotel</c>/<c>hotel_type</c>): Standard/Deluxe/Suite…
/// Gán (tuỳ chọn) vào <see cref="ServiceBooking.RoomClassId"/> khi đặt phòng. Thuần catalog, Name duy nhất/tenant.</summary>
public sealed class RoomClass : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int Status { get; set; }
}
