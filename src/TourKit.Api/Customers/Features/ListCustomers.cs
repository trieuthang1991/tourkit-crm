using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Customers.Features;

public sealed record ListCustomersQuery(int Page, int Size) : IQuery<Paged<CustomerResponse>>;

public sealed class ListCustomersHandler : IQueryHandler<ListCustomersQuery, Paged<CustomerResponse>>
{
    private readonly AppDbContext _db;

    public ListCustomersHandler(AppDbContext db) => _db = db;

    public async Task<Result<Paged<CustomerResponse>>> Handle(ListCustomersQuery q, CancellationToken ct)
    {
        var page = new PageQuery(q.Page, q.Size);
        var baseQuery = _db.Customers.AsNoTracking().OrderBy(c => c.FullName);

        var total = await baseQuery.CountAsync(ct);
        var items = await baseQuery
            .Skip(page.Skip).Take(page.SafeSize)
            .Select(c => new CustomerResponse(c.Id, c.FullName, c.Phone))
            .ToListAsync(ct);

        return new Paged<CustomerResponse>(items, total, page.SafePage, page.SafeSize);
    }
}
