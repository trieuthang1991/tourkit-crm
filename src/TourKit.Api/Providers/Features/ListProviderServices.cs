using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Providers.Features;

public sealed record ListProviderServicesQuery(int Page, int Size, Guid? ProviderId) : IQuery<Paged<ProviderServiceResponse>>;

public sealed class ListProviderServicesHandler : IQueryHandler<ListProviderServicesQuery, Paged<ProviderServiceResponse>>
{
    private readonly AppDbContext _db;

    public ListProviderServicesHandler(AppDbContext db) => _db = db;

    public async Task<Result<Paged<ProviderServiceResponse>>> Handle(ListProviderServicesQuery q, CancellationToken ct)
    {
        var page = new PageQuery(q.Page, q.Size);
        var baseQuery = _db.ProviderServices.AsNoTracking().AsQueryable();
        if (q.ProviderId is not null)
        {
            baseQuery = baseQuery.Where(x => x.ProviderId == q.ProviderId);
        }

        var ordered = baseQuery.OrderBy(p => p.CreatedAt);

        var total = await ordered.CountAsync(ct);
        var items = await ordered
            .Skip(page.Skip).Take(page.SafeSize)
            .Select(p => new ProviderServiceResponse(
                p.Id, p.ProviderId, p.ServiceItemId, p.PriceName, p.ContractPrice, p.PublicPrice,
                p.AmountOfPeople, p.Note, p.Status))
            .ToListAsync(ct);

        return new Paged<ProviderServiceResponse>(items, total, page.SafePage, page.SafeSize);
    }
}
