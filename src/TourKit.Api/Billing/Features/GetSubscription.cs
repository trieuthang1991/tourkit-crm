using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Billing.Features;

public sealed record GetSubscriptionQuery : IQuery<SubscriptionResponse>;

public sealed class GetSubscriptionHandler : IQueryHandler<GetSubscriptionQuery, SubscriptionResponse>
{
    private readonly AppDbContext _db;

    public GetSubscriptionHandler(AppDbContext db) => _db = db;

    public async Task<Result<SubscriptionResponse>> Handle(GetSubscriptionQuery q, CancellationToken ct)
    {
        var response = await (
            from s in _db.Subscriptions.AsNoTracking()
            join p in _db.Plans.AsNoTracking() on s.PlanId equals p.Id
            select new SubscriptionResponse(s.Id, s.PlanId, p.Code, s.Status, s.StartedAt, s.ExpiresAt))
            .FirstOrDefaultAsync(ct);

        return response is null ? Error.NotFound() : response;
    }
}
