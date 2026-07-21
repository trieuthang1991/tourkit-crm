using Microsoft.EntityFrameworkCore;
using TourKit.Application.Booking;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Domain;

namespace TourKit.Infrastructure.Booking;

/// <summary>
/// Query gộp cho màn Đơn hàng — dùng <c>AppDbContext</c> trực tiếp (repo riêng theo convention §5).
/// Cùng khuôn <c>ReportQueries</c>: 2 truy vấn top-level rồi ghép ở memory (SQLite-safe, tránh
/// subquery tương quan không dịch được).
/// </summary>
public sealed class OrderQueries(AppDbContext db) : IOrderQueries
{
    public async Task<IReadOnlyList<OrderStatsRowDto>> GetOrderStatsRowsAsync()
    {
        // CHIẾU đúng 4 cột — không materialize entity Order (nhiều cột, gồm cả jsonb).
        var orders = await db.Orders.AsNoTracking()
            .Select(o => new { o.Id, o.Status, o.OperationalStatus, o.TotalRevenue })
            .ToListAsync();

        // Phiếu thu đã duyệt: GỘP theo đơn NGAY Ở SQL, không kéo từng dòng về cộng tay.
        var paid = await db.ReceiptVouchers.AsNoTracking().Recognized()
            .GroupBy(r => r.OrderId)
            .Select(g => new { OrderId = g.Key, Amount = g.Sum(r => r.Amount) })
            .ToListAsync();

        var paidByOrder = paid.ToDictionary(x => x.OrderId, x => x.Amount);

        return orders
            .Select(o => new OrderStatsRowDto(
                (int)o.Status, (int)o.OperationalStatus, o.TotalRevenue, paidByOrder.GetValueOrDefault(o.Id)))
            .ToList();
    }
}
