using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Domain;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Commission.Features;

public sealed record GetOrderProfitQuery(Guid OrderId) : IQuery<OrderProfitResponse>;

public sealed class GetOrderProfitHandler : IQueryHandler<GetOrderProfitQuery, OrderProfitResponse>
{
    private readonly AppDbContext _db;

    public GetOrderProfitHandler(AppDbContext db) => _db = db;

    public async Task<Result<OrderProfitResponse>> Handle(GetOrderProfitQuery q, CancellationToken ct)
    {
        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == q.OrderId, ct);
        if (order is null)
        {
            return Error.NotFound();
        }

        var profit = OrderMath.Profit(order);
        return new OrderProfitResponse(order.TotalRevenue, order.TotalCost, profit);
    }
}
