using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Booking.Features;

/// <summary>Xác nhận chỗ (hệ cũ TR_TM_XNC): xoá đếm ngược → "chốt chỗ, không nhả".</summary>
public sealed record ConfirmSeatCommand(Guid SeatId) : ICommand<SeatResponse>;

public sealed class ConfirmSeatHandler : ICommandHandler<ConfirmSeatCommand, SeatResponse>
{
    private readonly AppDbContext _db;

    public ConfirmSeatHandler(AppDbContext db) => _db = db;

    public async Task<Result<SeatResponse>> Handle(ConfirmSeatCommand c, CancellationToken ct)
    {
        var seat = await _db.TourCustomers.FirstOrDefaultAsync(s => s.Id == c.SeatId, ct);
        if (seat is null)
        {
            return Error.NotFound();
        }

        if (seat.UpfrontAmount != 0m)
        {
            return Error.Validation("Chỉ xác nhận chỗ đang giữ (chưa đặt cọc).");
        }

        seat.HoldExpiresAt = null;   // chốt chỗ, không nhả
        await _db.SaveChangesAsync(ct);
        return SeatMapper.ToSeatResponse(seat);
    }
}
