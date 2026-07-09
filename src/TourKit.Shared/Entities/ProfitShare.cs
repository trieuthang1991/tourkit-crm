
namespace TourKit.Shared.Entities;

/// <summary>
/// Chia hoa hồng/lợi nhuận cho user theo đơn — grounded ở legacy bảng `ProfitSharing`
/// (UserId/TourId/Percentage/Comission/TotalRevenueByComission). ProfitBase lưu lại lợi nhuận đơn
/// (doanh thu − chi phí, xem <see cref="TourKit.Shared.Domain.OrderMath.Profit(TourKit.Shared.Entities.Order)"/>) TẠI THỜI ĐIỂM chia,
/// vì Order có thể thay đổi doanh thu/chi phí sau đó — legacy field TotalRevenueByComission.
/// Amount tương ứng legacy field Comission.
/// </summary>
public sealed class ProfitShare : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid OrderId { get; set; }
    public Guid UserId { get; set; }
    public decimal Percentage { get; set; }
    public decimal Amount { get; set; }
    public decimal ProfitBase { get; set; }
}
