using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Domain;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Finance.Features;

public sealed record GetOrderBalanceQuery(Guid OrderId) : IQuery<OrderBalanceResponse>;

public sealed class GetOrderBalanceHandler : IQueryHandler<GetOrderBalanceQuery, OrderBalanceResponse>
{
    private readonly AppDbContext _db;

    public GetOrderBalanceHandler(AppDbContext db) => _db = db;

    public async Task<Result<OrderBalanceResponse>> Handle(GetOrderBalanceQuery q, CancellationToken ct)
    {
        var order = await _db.Orders.AsNoTracking()
            .Where(o => o.Id == q.OrderId)
            .Select(o => new { o.TotalRevenue })
            .FirstOrDefaultAsync(ct);
        if (order is null)
        {
            return Error.NotFound();
        }

        // Chỉ phiếu ĐÃ DUYỆT mới tính (quy tắc ở ReceiptQueries.Recognized — một chỗ).
        var paid = await _db.ReceiptVouchers
            .Where(r => r.OrderId == q.OrderId).Recognized()
            .SumAsync(r => r.Amount, ct);

        return new OrderBalanceResponse(q.OrderId, order.TotalRevenue, paid, order.TotalRevenue - paid);
    }
}
