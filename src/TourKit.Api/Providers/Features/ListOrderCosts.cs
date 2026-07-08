using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Providers.Features;

public sealed record ListOrderCostsQuery(Guid OrderId) : IQuery<IReadOnlyList<OrderCostResponse>>;

public sealed class ListOrderCostsHandler : IQueryHandler<ListOrderCostsQuery, IReadOnlyList<OrderCostResponse>>
{
    private readonly AppDbContext _db;

    public ListOrderCostsHandler(AppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<OrderCostResponse>>> Handle(ListOrderCostsQuery q, CancellationToken ct)
    {
        IReadOnlyList<OrderCostResponse> costs = await _db.OrderCosts.AsNoTracking()
            .Where(c => c.OrderId == q.OrderId)
            .OrderBy(c => c.DayIndex)
            .Select(c => new OrderCostResponse(
                c.Id, c.OrderId, c.ProviderId, c.ServiceName, c.DayIndex,
                c.ExpectedAmount, c.ActualAmount, c.Deposit, c.Surcharge, c.Vat, c.Status))
            .ToListAsync(ct);

        return Result.Success(costs);
    }
}
