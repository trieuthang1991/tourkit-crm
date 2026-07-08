using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Finance.Features;

public sealed record ListPaymentsQuery(Guid OrderId) : IQuery<IReadOnlyList<PaymentResponse>>;

public sealed class ListPaymentsHandler : IQueryHandler<ListPaymentsQuery, IReadOnlyList<PaymentResponse>>
{
    private readonly AppDbContext _db;

    public ListPaymentsHandler(AppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<PaymentResponse>>> Handle(ListPaymentsQuery q, CancellationToken ct)
    {
        IReadOnlyList<PaymentResponse> payments = await _db.PaymentVouchers.AsNoTracking()
            .Where(p => p.OrderId == q.OrderId)
            .OrderBy(p => p.IssuedAt)
            .Select(p => new PaymentResponse(
                p.Id, p.Code, p.OrderId, p.ProviderId, p.OrderCostId, p.Amount, p.PaymentMethod,
                p.IssuedAt, p.Partner, p.ReceiverName, p.Note, p.Status, p.IsRecognized))
            .ToListAsync(ct);

        return Result.Success(payments);
    }
}
