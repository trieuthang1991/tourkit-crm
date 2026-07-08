using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Providers.Features;

public sealed record GetProviderQuery(Guid Id) : IQuery<ProviderResponse>;

public sealed class GetProviderHandler : IQueryHandler<GetProviderQuery, ProviderResponse>
{
    private readonly AppDbContext _db;

    public GetProviderHandler(AppDbContext db) => _db = db;

    public async Task<Result<ProviderResponse>> Handle(GetProviderQuery q, CancellationToken ct)
    {
        var provider = await _db.Providers.AsNoTracking()
            .Where(p => p.Id == q.Id)
            .Select(p => new ProviderResponse(
                p.Id, p.Code, p.Name, p.Type, p.Phone, p.Email, p.Address,
                p.TaxCode, p.ContactPerson, p.BankAccount, p.BankName, p.Rate, p.Status))
            .FirstOrDefaultAsync(ct);

        return provider is null ? Error.NotFound() : provider;
    }
}
