using TourKit.Infrastructure.Entities;

namespace TourKit.Infrastructure.Domain;

/// <summary>
/// CÔNG THỨC TÍNH CHI PHÍ ĐƠN HÀNG — một chỗ duy nhất. Đổi cách tính thì sửa ở ĐÂY,
/// mọi nơi (Order.TotalCost) đều dùng chung.
/// </summary>
public static class OrderMath
{
    /// <summary>Tổng chi phí thực trả NCC của 1 đơn: Σ ActualAmount các dòng OrderCost.</summary>
    public static decimal TotalCost(IEnumerable<OrderCost> costs) => costs.Sum(c => c.ActualAmount);
}
