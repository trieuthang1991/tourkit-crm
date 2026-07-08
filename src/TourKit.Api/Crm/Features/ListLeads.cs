using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Crm.Features;

public sealed record ListLeadsQuery(int Page, int Size) : IQuery<Paged<LeadResponse>>;

public sealed class ListLeadsHandler : IQueryHandler<ListLeadsQuery, Paged<LeadResponse>>
{
    private readonly AppDbContext _db;

    public ListLeadsHandler(AppDbContext db) => _db = db;

    public async Task<Result<Paged<LeadResponse>>> Handle(ListLeadsQuery q, CancellationToken ct)
    {
        var page = new PageQuery(q.Page, q.Size);
        var baseQuery = _db.Leads.AsNoTracking().OrderByDescending(l => l.CreatedAt);

        var total = await baseQuery.CountAsync(ct);
        var items = await baseQuery
            .Skip(page.Skip).Take(page.SafeSize)
            .Select(l => new LeadResponse(
                l.Id, l.FullName, l.Phone, l.Email, l.Source, l.Status, l.AssignedToUserId, l.ConvertedCustomerId))
            .ToListAsync(ct);

        return new Paged<LeadResponse>(items, total, page.SafePage, page.SafeSize);
    }
}
