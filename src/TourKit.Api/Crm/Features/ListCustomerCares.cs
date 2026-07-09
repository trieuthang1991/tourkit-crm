using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Crm.Features;

public sealed record ListCustomerCaresQuery(int Page, int Size) : IQuery<Paged<CustomerCareResponse>>;

public sealed class ListCustomerCaresHandler : IQueryHandler<ListCustomerCaresQuery, Paged<CustomerCareResponse>>
{
    private readonly AppDbContext _db;

    public ListCustomerCaresHandler(AppDbContext db) => _db = db;

    public async Task<Result<Paged<CustomerCareResponse>>> Handle(ListCustomerCaresQuery q, CancellationToken ct)
    {
        var page = new PageQuery(q.Page, q.Size);
        var baseQuery = _db.CustomerCares.AsNoTracking().OrderByDescending(c => c.CreatedAt);

        var total = await baseQuery.CountAsync(ct);
        var items = await baseQuery
            .Skip(page.Skip).Take(page.SafeSize)
            .Select(c => new CustomerCareResponse(
                c.Id, c.CustomerId, c.Title, c.Detail, c.RemindAt, c.Feedback, c.AssignedToUserId, c.Status))
            .ToListAsync(ct);

        return new Paged<CustomerCareResponse>(items, total, page.SafePage, page.SafeSize);
    }
}
