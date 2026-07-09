using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Commission.Features;

public sealed record ListCommissionRulesQuery(int Page, int Size) : IQuery<Paged<CommissionRuleResponse>>;

public sealed class ListCommissionRulesHandler : IQueryHandler<ListCommissionRulesQuery, Paged<CommissionRuleResponse>>
{
    private readonly AppDbContext _db;

    public ListCommissionRulesHandler(AppDbContext db) => _db = db;

    public async Task<Result<Paged<CommissionRuleResponse>>> Handle(ListCommissionRulesQuery q, CancellationToken ct)
    {
        var page = new PageQuery(q.Page, q.Size);
        var baseQuery = _db.CommissionRules.AsNoTracking().OrderByDescending(r => r.CreatedAt);

        var total = await baseQuery.CountAsync(ct);
        var items = await baseQuery
            .Skip(page.Skip).Take(page.SafeSize)
            .Select(r => new CommissionRuleResponse(r.Id, r.UserId, r.Percentage, r.Status))
            .ToListAsync(ct);

        return new Paged<CommissionRuleResponse>(items, total, page.SafePage, page.SafeSize);
    }
}
