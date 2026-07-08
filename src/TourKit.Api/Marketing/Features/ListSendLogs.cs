using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Marketing.Features;

public sealed record ListSendLogsQuery(Guid CampaignId) : IQuery<IReadOnlyList<SendLogResponse>>;

public sealed class ListSendLogsHandler : IQueryHandler<ListSendLogsQuery, IReadOnlyList<SendLogResponse>>
{
    private readonly AppDbContext _db;

    public ListSendLogsHandler(AppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<SendLogResponse>>> Handle(ListSendLogsQuery q, CancellationToken ct)
    {
        IReadOnlyList<SendLogResponse> logs = await _db.MarketingSendLogs.AsNoTracking()
            .Where(l => l.CampaignId == q.CampaignId)
            .OrderByDescending(l => l.SentAt)
            .Select(l => new SendLogResponse(l.Id, l.Recipient, l.Status, l.SentAt))
            .ToListAsync(ct);

        return Result.Success(logs);
    }
}
