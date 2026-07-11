using TourKit.Shared.Enums;

namespace TourKit.Shared.Entities;

/// <summary>
/// Luồng duyệt nhiều cấp cho phiếu chi (đối xứng <see cref="ReceiptApproval"/>) — ALTERNATIVE cho duyệt 1 cấp
/// (PaymentsController.approve). Mỗi PaymentVoucher có tối đa 1 luồng duyệt đang tồn tại.
/// </summary>
public sealed class PaymentApproval : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid PaymentVoucherId { get; set; }
    public ApprovalMethod Method { get; set; }
    public int CurrentStepOrder { get; set; }
    public ApprovalStatus Status { get; set; }
}
