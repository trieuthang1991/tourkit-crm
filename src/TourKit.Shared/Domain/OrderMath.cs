using TourKit.Shared.Entities;

namespace TourKit.Shared.Domain;

/// <summary>
/// CÔNG THỨC TÍNH CHI PHÍ ĐƠN HÀNG — một chỗ duy nhất. Đổi cách tính thì sửa ở ĐÂY,
/// mọi nơi (Order.TotalCost) đều dùng chung.
/// </summary>
public static class OrderMath
{
    /// <summary>Tổng chi phí thực trả NCC của 1 đơn: Σ ActualAmount các dòng OrderCost.</summary>
    public static decimal TotalCost(IEnumerable<OrderCost> costs) => costs.Sum(c => c.ActualAmount);

    /// <summary>Lợi nhuận đơn = doanh thu − chi phí. Base để chia hoa hồng (legacy TotalRevenueByComission).</summary>
    public static decimal Profit(Order o) => o.TotalRevenue - o.TotalCost;

    /// <summary>Lợi nhuận từ doanh thu &amp; chi phí rời (dùng ở báo cáo khi chưa có Order tracked).</summary>
    public static decimal Profit(decimal revenue, decimal cost) => revenue - cost;

    /// <summary>Công nợ còn lại = tổng phải − đã (thu/chi ĐÃ ghi nhận). Dùng cho công nợ phải thu &amp; phải trả.</summary>
    public static decimal Outstanding(decimal total, decimal settled) => total - settled;
}
