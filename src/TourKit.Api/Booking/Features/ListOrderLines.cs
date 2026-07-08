using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Booking.Features;

public sealed record ListOrderLinesQuery(Guid OrderId) : IQuery<IReadOnlyList<BookingLineResponse>>;

public sealed class ListOrderLinesHandler : IQueryHandler<ListOrderLinesQuery, IReadOnlyList<BookingLineResponse>>
{
    private readonly AppDbContext _db;

    public ListOrderLinesHandler(AppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<BookingLineResponse>>> Handle(ListOrderLinesQuery q, CancellationToken ct)
    {
        IReadOnlyList<BookingLineResponse> lines = await _db.TourCustomers.AsNoTracking()
            .Where(l => l.OrderId == q.OrderId)
            .OrderBy(l => l.CreatedAt)
            .Select(l => new BookingLineResponse(
                l.Id, l.Quantity, l.AmountChildren, l.AmountChildrenSmall, l.QuantityBaby,
                l.PriceAdult, l.PriceChild, l.PriceChildSmall, l.PriceBaby,
                l.UpfrontAmount, l.ReservationCode, l.IsMainContact))
            .ToListAsync(ct);

        return Result.Success(lines);
    }
}
