using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Booking.Features;

public sealed record ListOrdersQuery(int Page, int Size) : IQuery<Paged<OrderResponse>>;

public sealed class ListOrdersHandler : IQueryHandler<ListOrdersQuery, Paged<OrderResponse>>
{
    private readonly AppDbContext _db;

    public ListOrdersHandler(AppDbContext db) => _db = db;

    public async Task<Result<Paged<OrderResponse>>> Handle(ListOrdersQuery q, CancellationToken ct)
    {
        var page = new PageQuery(q.Page, q.Size);
        var baseQuery = _db.Orders.AsNoTracking().OrderByDescending(o => o.CreatedAt);

        var total = await baseQuery.CountAsync(ct);
        var items = await baseQuery
            .Skip(page.Skip).Take(page.SafeSize)
            .Select(o => new OrderResponse(
                o.Id, o.Code, o.TourDepartureId, o.CustomerId, o.TotalRevenue, o.TotalCost, o.Status))
            .ToListAsync(ct);

        return new Paged<OrderResponse>(items, total, page.SafePage, page.SafeSize);
    }
}
