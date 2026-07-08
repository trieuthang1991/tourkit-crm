using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Providers.Features;

public sealed record ListProvidersQuery(int Page, int Size) : IQuery<Paged<ProviderResponse>>;

public sealed class ListProvidersHandler : IQueryHandler<ListProvidersQuery, Paged<ProviderResponse>>
{
    private readonly AppDbContext _db;

    public ListProvidersHandler(AppDbContext db) => _db = db;

    public async Task<Result<Paged<ProviderResponse>>> Handle(ListProvidersQuery q, CancellationToken ct)
    {
        var page = new PageQuery(q.Page, q.Size);
        var baseQuery = _db.Providers.AsNoTracking().OrderBy(p => p.Name);

        var total = await baseQuery.CountAsync(ct);
        var items = await baseQuery
            .Skip(page.Skip).Take(page.SafeSize)
            .Select(p => new ProviderResponse(
                p.Id, p.Code, p.Name, p.Type, p.Phone, p.Email, p.Address,
                p.TaxCode, p.ContactPerson, p.BankAccount, p.BankName, p.Rate, p.Status))
            .ToListAsync(ct);

        return new Paged<ProviderResponse>(items, total, page.SafePage, page.SafeSize);
    }
}
