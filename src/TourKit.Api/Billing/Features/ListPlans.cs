using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Billing.Features;

public sealed record ListPlansQuery : IQuery<IReadOnlyList<PlanResponse>>;

public sealed class ListPlansHandler : IQueryHandler<ListPlansQuery, IReadOnlyList<PlanResponse>>
{
    private readonly AppDbContext _db;

    public ListPlansHandler(AppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<PlanResponse>>> Handle(ListPlansQuery q, CancellationToken ct)
    {
        IReadOnlyList<PlanResponse> plans = await _db.Plans.AsNoTracking()
            .OrderBy(p => p.PriceMonthly)
            .Select(p => new PlanResponse(p.Id, p.Code, p.Name, p.MaxUsers, p.MaxTours, p.PriceMonthly))
            .ToListAsync(ct);

        return Result.Success(plans);
    }
}
