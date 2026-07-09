
namespace TourKit.Shared.Entities;

/// <summary>Quy tắc hoa hồng sales (legacy Comission): 1 user → 1 % hoa hồng trên lợi nhuận đơn.
/// Chiều id_customer_type + CommissionCampaign của legacy DEFERRED (Customer chưa có CustomerType).</summary>
public sealed class CommissionRule : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public decimal Percentage { get; set; }
    public int Status { get; set; }
}
