namespace TourKit.Shared.Entities;

/// <summary>
/// Thu-chi của HDV trong chuyến (legacy <c>RevenueExpensesInTourGuide</c>): HDV thu hộ/bán thêm và chi hộ
/// (vé vào cửa, tip, ăn) khi dẫn tour. Tổng hợp net để đối soát tạm ứng. Không phụ thuộc ngoài.
/// </summary>
public sealed class GuideTransaction : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid TourGuideAssignmentId { get; set; }
    public int Type { get; set; }                      // GuideTransactionType (0 thu, 1 chi)
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
}
