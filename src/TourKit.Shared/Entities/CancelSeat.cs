
namespace TourKit.Shared.Entities;

/// <summary>Huỷ chỗ (legacy CancelSeats) — ghi lý do + hoàn tiền cho một dòng TourCustomer.</summary>
public sealed class CancelSeat : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid TourCustomerId { get; set; }
    public Guid OrderId { get; set; }
    public string? Note { get; set; }
    public decimal RefundAmount { get; set; }
    public decimal RefundRemain { get; set; }
    public decimal RefundPercentage { get; set; }
    public int Status { get; set; }
}
