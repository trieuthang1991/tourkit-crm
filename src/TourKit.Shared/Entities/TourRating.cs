
namespace TourKit.Shared.Entities;

/// <summary>Đánh giá sau tour (legacy Rate): số sao + nhận xét theo chuyến, có thể gắn đơn.</summary>
public sealed class TourRating : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid? TourDepartureId { get; set; }   // Rate.TourId
    public Guid? OrderId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public int Stars { get; set; }               // 1..5 (Rate.AvgStar rút gọn)
    public string? Comment { get; set; }
    public int Status { get; set; }
}
