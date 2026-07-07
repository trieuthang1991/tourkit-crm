namespace TourKit.Infrastructure.Entities;

public sealed class TourDeparture : Tour
{
    public TourDeparture() => Kind = TourKind.Departure;

    public int AmountAdults { get; set; }
    public int AmountChildren { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public bool IsClosed { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
}
