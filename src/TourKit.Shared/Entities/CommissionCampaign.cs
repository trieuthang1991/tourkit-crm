namespace TourKit.Shared.Entities;

/// <summary>
/// Chính sách hoa hồng theo mốc / bậc thang (legacy <c>CommissionCompaign</c>): một CHÍNH SÁCH có tên,
/// khoảng thời gian áp dụng (<see cref="StartDate"/>..<see cref="EndDate"/>), gán cho một nhóm nhân viên
/// (<see cref="CommissionCampaignUser"/>) và gồm nhiều bậc lợi nhuận (<see cref="CommissionTier"/>).
/// Khác <see cref="CommissionRule"/> (1 user → 1 % phẳng): ở đây % thay đổi theo mốc lợi nhuận đạt được.
/// Ràng buộc: hai chính sách ĐANG ÁP DỤNG có chung nhân viên KHÔNG được chồng khoảng thời gian.
/// </summary>
public sealed class CommissionCampaign : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
    public int Status { get; set; }   // 0 đang áp dụng, 1 ngừng
}

/// <summary>Nhân viên được áp chính sách hoa hồng bậc thang (legacy bảng nối compaign↔user).</summary>
public sealed class CommissionCampaignUser : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid CommissionCampaignId { get; set; }
    public Guid UserId { get; set; }
}

/// <summary>
/// Một bậc lợi nhuận của <see cref="CommissionCampaign"/> (legacy <c>CommissionProfitLevel</c>):
/// khoảng lợi nhuận [<see cref="StartAmount"/>, <see cref="EndAmount"/>) hưởng <see cref="Percentage"/> %.
/// Các bậc sắp theo <see cref="StartAmount"/> tăng dần; bậc cuối coi như mở (không chặn trên).
/// </summary>
public sealed class CommissionTier : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid CommissionCampaignId { get; set; }
    public decimal StartAmount { get; set; }
    public decimal EndAmount { get; set; }
    public decimal Percentage { get; set; }
}
