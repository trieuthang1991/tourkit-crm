using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Commission.Features;

public sealed record ListProfitSharesQuery(Guid OrderId) : IQuery<IReadOnlyList<ProfitShareResponse>>;

public sealed class ListProfitSharesHandler : IQueryHandler<ListProfitSharesQuery, IReadOnlyList<ProfitShareResponse>>
{
    private readonly AppDbContext _db;

    public ListProfitSharesHandler(AppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<ProfitShareResponse>>> Handle(ListProfitSharesQuery q, CancellationToken ct)
    {
        IReadOnlyList<ProfitShareResponse> shares = await _db.ProfitShares.AsNoTracking()
            .Where(s => s.OrderId == q.OrderId)
            .Select(s => new ProfitShareResponse(
                s.Id, s.OrderId, s.UserId, s.Percentage, s.Amount, s.ProfitBase))
            .ToListAsync(ct);

        return Result.Success(shares);
    }
}
