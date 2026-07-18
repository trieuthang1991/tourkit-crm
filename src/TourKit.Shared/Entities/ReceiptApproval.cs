
using TourKit.Shared.Enums;

namespace TourKit.Shared.Entities;

/// <summary>
/// Luồng duyệt nhiều cấp cho phiếu thu (legacy ReceiptVoucherApproval) — ALTERNATIVE cho duyệt 1 cấp
/// (ReceiptEndpoints.approve). Mỗi ReceiptVoucher có tối đa 1 luồng duyệt đang tồn tại.
/// </summary>
public sealed class ReceiptApproval : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ReceiptVoucherId { get; set; }

    /// <summary>Template <see cref="ApprovalProcess"/> đã được SNAPSHOT để dựng luồng này (null nếu dựng trực tiếp từ dto.Steps).</summary>
    public Guid? ApprovalProcessId { get; set; }

    public ApprovalMethod Method { get; set; }
    public int CurrentStepOrder { get; set; }
    public ApprovalStatus Status { get; set; }
}
