using TourKit.Shared.Enums;

namespace TourKit.Shared.Entities;

public sealed class TourDeparture : Tour
{
    public TourDeparture() => Kind = TourKind.Departure;

    public int AmountAdults { get; set; }
    public int AmountChildren { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public bool IsClosed { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }

    // Chốt sổ hoa hồng — legacy tours.StatusComission / DateClosedComission.
    // Khoá hoa hồng của chuyến: đã chốt thì không tính lại/không sửa số đã quyết toán.
    public bool CommissionClosed { get; set; }
    public DateTimeOffset? CommissionClosedAt { get; set; }
}
