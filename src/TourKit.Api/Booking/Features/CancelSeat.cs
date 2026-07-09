using Microsoft.EntityFrameworkCore;
using TourKit.Shared.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Booking.Features;

/// <summary>Huỷ chỗ + hoàn tiền (legacy CancelSeats + statusCancel != 0).</summary>
public sealed record CancelSeatCommand(Guid SeatId, string? Note, decimal RefundAmount) : ICommand<SeatResponse>;

public sealed class CancelSeatHandler : ICommandHandler<CancelSeatCommand, SeatResponse>
{
    private readonly AppDbContext _db;

    public CancelSeatHandler(AppDbContext db) => _db = db;

    public async Task<Result<SeatResponse>> Handle(CancelSeatCommand c, CancellationToken ct)
    {
        var seat = await _db.TourCustomers.FirstOrDefaultAsync(s => s.Id == c.SeatId, ct);
        if (seat is null)
        {
            return Error.NotFound();
        }

        if (seat.Status != 0)
        {
            return Error.Conflict("Chỗ đã được huỷ.");
        }

        _db.CancelSeats.Add(new CancelSeat
        {
            TourCustomerId = seat.Id,
            OrderId = seat.OrderId,
            Note = c.Note,
            RefundAmount = c.RefundAmount,
            RefundRemain = seat.UpfrontAmount - c.RefundAmount,
            RefundPercentage = seat.UpfrontAmount > 0m
                ? Math.Round(c.RefundAmount / seat.UpfrontAmount * 100m, 2)
                : 0m,
        });
        seat.Status = 1;   // statusCancel != 0 → đã huỷ
        seat.HoldExpiresAt = null;
        await _db.SaveChangesAsync(ct);
        return SeatMapper.ToSeatResponse(seat);
    }
}
