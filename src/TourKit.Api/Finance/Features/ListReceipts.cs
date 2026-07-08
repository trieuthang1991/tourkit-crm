using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Finance.Features;

public sealed record ListReceiptsQuery(Guid OrderId) : IQuery<IReadOnlyList<ReceiptResponse>>;

public sealed class ListReceiptsHandler : IQueryHandler<ListReceiptsQuery, IReadOnlyList<ReceiptResponse>>
{
    private readonly AppDbContext _db;

    public ListReceiptsHandler(AppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<ReceiptResponse>>> Handle(ListReceiptsQuery q, CancellationToken ct)
    {
        IReadOnlyList<ReceiptResponse> receipts = await _db.ReceiptVouchers.AsNoTracking()
            .Where(r => r.OrderId == q.OrderId)
            .OrderBy(r => r.IssuedAt)
            .Select(r => new ReceiptResponse(
                r.Id, r.Code, r.OrderId, r.Amount, r.PaymentMethod, r.IssuedAt, r.Partner, r.Note, r.Status, r.IsRecognized))
            .ToListAsync(ct);

        return Result.Success(receipts);
    }
}
