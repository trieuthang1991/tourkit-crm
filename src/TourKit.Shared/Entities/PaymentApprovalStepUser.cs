using TourKit.Shared.Enums;

namespace TourKit.Shared.Entities;

/// <summary>Một người duyệt được phân công ở một bước cụ thể của <see cref="PaymentApproval"/> (đối xứng ReceiptApprovalStepUser).</summary>
public sealed class PaymentApprovalStepUser : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid PaymentApprovalId { get; set; }
    public Guid PaymentVoucherId { get; set; }
    public int StepOrder { get; set; }
    public Guid UserId { get; set; }
    public StepUserStatus Status { get; set; }
    public DateTimeOffset? ActedAt { get; set; }
    public string? Note { get; set; }
}
