namespace TourKit.Shared.Entities;

/// <summary>
/// Phân công HDV cho chuyến (legacy bảng <c>TourGuide</c>): HDV là một <see cref="Provider"/> loại Guide,
/// gắn vào một chuyến (<see cref="TourDeparture"/>) kèm giờ đi/về/trả tour.
/// Status bám legacy State: 1=Created, 2=Active, 4=Delete. Ký xác nhận/quỹ vé/thu-chi hộ (SignatureGuide,
/// TicketFund, RevenueExpenses) là phần "trả tour" nâng cao — deferred, cần requirement.
/// </summary>
public sealed class TourGuideAssignment : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid TourDepartureId { get; set; }        // legacy tour_id
    public Guid ProviderId { get; set; }             // legacy provider_id (HDV = Provider type Guide)
    public DateTimeOffset? TimeGo { get; set; }       // giờ khởi hành
    public DateTimeOffset? TimeCome { get; set; }     // giờ kết thúc (>= TimeGo)
    public DateTimeOffset? TimeReturn { get; set; }   // giờ trả tour/hoàn tất
    public string? Note { get; set; }
    public int Status { get; set; } = 1;              // legacy State: 1=Created, 2=Active, 4=Delete
}
