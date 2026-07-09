using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Providers.Features;

public sealed record ListServiceItemsQuery(int Page, int Size) : IQuery<Paged<ServiceItemResponse>>;

public sealed class ListServiceItemsHandler : IQueryHandler<ListServiceItemsQuery, Paged<ServiceItemResponse>>
{
    private readonly AppDbContext _db;

    public ListServiceItemsHandler(AppDbContext db) => _db = db;

    public async Task<Result<Paged<ServiceItemResponse>>> Handle(ListServiceItemsQuery q, CancellationToken ct)
    {
        var page = new PageQuery(q.Page, q.Size);
        var baseQuery = _db.ServiceItems.AsNoTracking().OrderBy(s => s.Name);

        var total = await baseQuery.CountAsync(ct);
        var items = await baseQuery
            .Skip(page.Skip).Take(page.SafeSize)
            .Select(s => new ServiceItemResponse(s.Id, s.Code, s.Name, s.Category, s.Status))
            .ToListAsync(ct);

        return new Paged<ServiceItemResponse>(items, total, page.SafePage, page.SafeSize);
    }
}
