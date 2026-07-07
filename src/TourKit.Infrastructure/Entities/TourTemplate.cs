namespace TourKit.Infrastructure.Entities;

public sealed class TourTemplate : Tour
{
    public TourTemplate() => Kind = TourKind.Template;

    public int ReservationHours { get; set; }          // thời hạn giữ chỗ (giờ)
    public decimal PriceAdult { get; set; }
    public decimal PriceChild { get; set; }
    public decimal PriceChildSmall { get; set; }
    public decimal PriceBaby { get; set; }
    public string? TermsNote { get; set; }
    public string? TermsNoteEn { get; set; }
}
