using Microsoft.EntityFrameworkCore;
using TourKit.Application.Customers;
using TourKit.Infrastructure.Persistence;

namespace TourKit.Infrastructure.Customers;

/// <summary>Query gộp cho Khách hàng — đếm số người mua lần đầu/mua lại ở SQL (C1). Query filter tenant tự áp.</summary>
public sealed class CustomerQueries(AppDbContext db) : ICustomerQueries
{
    public async Task<(int FirstTime, int Repeat)> BuyerCountsAsync()
    {
        // GROUP BY CustomerId → số đơn mỗi khách; đếm nhóm =1 và >1. SQLite/Postgres/InMemory đều dịch được.
        var counts = await db.Orders
            .GroupBy(o => o.CustomerId)
            .Select(g => g.Count())
            .ToListAsync();

        return (counts.Count(c => c == 1), counts.Count(c => c > 1));
    }
}
