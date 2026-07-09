
namespace TourKit.Shared.Entities;

public sealed class TourItinerary : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid TourId { get; set; }                   // trỏ tới Tour (Template hoặc Departure)
    public int DayIndex { get; set; }                  // ngày thứ mấy
    public string Title { get; set; } = string.Empty;
    public string? Detail { get; set; }
}
