using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Finance.Features;

public sealed record GetApprovalQuery(Guid ReceiptId) : IQuery<ApprovalResponse>;

public sealed class GetApprovalHandler : IQueryHandler<GetApprovalQuery, ApprovalResponse>
{
    private readonly AppDbContext _db;

    public GetApprovalHandler(AppDbContext db) => _db = db;

    public async Task<Result<ApprovalResponse>> Handle(GetApprovalQuery q, CancellationToken ct)
    {
        var approval = await _db.ReceiptApprovals.AsNoTracking()
            .FirstOrDefaultAsync(a => a.ReceiptVoucherId == q.ReceiptId, ct);
        if (approval is null)
        {
            return Error.NotFound();
        }

        return await ApprovalResponseBuilder.BuildAsync(_db, approval.Id, ct);
    }
}
