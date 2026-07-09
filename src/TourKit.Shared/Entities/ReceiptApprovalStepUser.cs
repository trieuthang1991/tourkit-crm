
using TourKit.Shared.Enums;

namespace TourKit.Shared.Entities;

/// <summary>Một người duyệt được phân công ở một bước cụ thể của ReceiptApproval (legacy ReceiptVoucherApprovalStepUser).</summary>
public sealed class ReceiptApprovalStepUser : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ReceiptApprovalId { get; set; }
    public Guid ReceiptVoucherId { get; set; }
    public int StepOrder { get; set; }
    public Guid UserId { get; set; }
    public StepUserStatus Status { get; set; }
    public DateTimeOffset? ActedAt { get; set; }
    public string? Note { get; set; }
}
