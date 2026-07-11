namespace TourKit.Shared.Entities;

/// <summary>
/// Danh mục lý do chuyển chuyến (legacy <c>ReasonSwitch</c>/<c>DetailReasonSwitch</c>): lý do chuẩn để
/// chọn khi chuyển đơn sang chuyến khác (<see cref="TourTransfer.ReasonId"/>) — thống kê lý do đổi lịch.
/// <see cref="Name"/> duy nhất theo tenant. Thuần catalog.
/// </summary>
public sealed class TransferReason : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int Status { get; set; }
}
