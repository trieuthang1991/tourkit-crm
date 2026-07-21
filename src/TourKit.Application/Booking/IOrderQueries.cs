namespace TourKit.Application.Booking;

/// <summary>
/// Một dòng THÔ để dựng thẻ thống kê màn Đơn hàng: chỉ 4 trường cần cho phép gộp, KHÔNG phải cả entity.
/// </summary>
public sealed record OrderStatsRowDto(int Status, int OperationalStatus, decimal TotalRevenue, decimal Paid);

/// <summary>
/// Query gộp nhiều bảng cho màn Đơn hàng (convention §5: query phức tạp → interface repo riêng,
/// <c>IRepository&lt;T&gt;</c> generic không diễn đạt được GROUP BY/SUM). Impl ở Infrastructure.
/// </summary>
public interface IOrderQueries
{
    /// <summary>
    /// Dòng thô cho thẻ thống kê màn Đơn hàng: Orders CHIẾU đúng 4 cột (không kéo cả entity, Order
    /// có nhiều cột kể cả jsonb) + phiếu thu đã duyệt GỘP SẴN theo đơn ở SQL.
    /// </summary>
    Task<IReadOnlyList<OrderStatsRowDto>> GetOrderStatsRowsAsync();
}
