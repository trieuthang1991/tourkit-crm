namespace TourKit.Shared.Entities;

/// <summary>
/// Lịch sử chuyển chuyến của 1 đơn (legacy <c>TransferHistory</c>/<c>CustomerHistoryChangeTour</c>):
/// dời khách sang chuyến khác (đổi lịch), ghi chuyến nguồn/đích + lý do + thời điểm. Không phụ thuộc ngoài.
/// </summary>
public sealed class TourTransfer : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid OrderId { get; set; }
    public Guid FromDepartureId { get; set; }
    public Guid ToDepartureId { get; set; }
    public string? Reason { get; set; }
    public Guid? ReasonId { get; set; }                // lý do chuẩn từ danh mục TransferReason (tuỳ chọn)
    public DateTimeOffset TransferredAt { get; set; }
}
