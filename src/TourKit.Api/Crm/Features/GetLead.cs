using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Crm.Features;

public sealed record GetLeadQuery(Guid Id) : IQuery<LeadResponse>;

public sealed class GetLeadHandler : IQueryHandler<GetLeadQuery, LeadResponse>
{
    private readonly AppDbContext _db;

    public GetLeadHandler(AppDbContext db) => _db = db;

    public async Task<Result<LeadResponse>> Handle(GetLeadQuery q, CancellationToken ct)
    {
        var lead = await _db.Leads.AsNoTracking()
            .Where(l => l.Id == q.Id)
            .Select(l => new LeadResponse(
                l.Id, l.FullName, l.Phone, l.Email, l.Source, l.Status, l.AssignedToUserId, l.ConvertedCustomerId))
            .FirstOrDefaultAsync(ct);

        return lead is null ? Error.NotFound() : lead;
    }
}
