using TourKit.Shared.Enums;

namespace TourKit.Shared.Entities;

/// <summary>
/// Quy trình duyệt CẤU HÌNH được (legacy <c>ApprovalProcess</c>): TEMPLATE tái sử dụng — định nghĩa một
/// chuỗi bước duyệt (<see cref="ApprovalProcessStep"/>) theo chức vụ, mỗi bước gán người duyệt cụ thể.
/// Khác luồng duyệt cụ thể theo phiếu (PaymentApproval/ReceiptApproval): đây là lớp ĐỊNH NGHĨA, dùng để
/// admin dựng quy trình như dữ liệu (không hard-code số cấp/người duyệt).
/// </summary>
public sealed class ApprovalProcess : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ApprovalMethod Method { get; set; } = ApprovalMethod.One;  // One = một người bước đó duyệt là qua; All = tất cả
    public int Status { get; set; }                                   // 0 đang dùng, 1 ngừng
}

/// <summary>Một bước trong <see cref="ApprovalProcess"/>: thứ tự + chức vụ đảm nhận (legacy <c>ApprovalStep</c>).</summary>
public sealed class ApprovalProcessStep : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ApprovalProcessId { get; set; }
    public int StepOrder { get; set; }
    public Guid PositionId { get; set; }
}

/// <summary>Người duyệt được phân vào một bước (legacy <c>ApprovalStepUser</c>).</summary>
public sealed class ApprovalProcessStepUser : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ApprovalProcessStepId { get; set; }
    public Guid UserId { get; set; }
}
