using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Booking.Features;

/// <summary>Đặt cọc: cộng vào upfront_amount của chỗ. (Đối soát với phiếu thu = follow-up Finance.)</summary>
public sealed record DepositSeatCommand(Guid SeatId, decimal Amount) : ICommand<SeatResponse>;

public sealed class DepositSeatValidator : AbstractValidator<DepositSeatCommand>
{
    public DepositSeatValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0m).WithMessage("Số tiền cọc phải lớn hơn 0.");
    }
}

public sealed class DepositSeatHandler : ICommandHandler<DepositSeatCommand, SeatResponse>
{
    private readonly AppDbContext _db;

    public DepositSeatHandler(AppDbContext db) => _db = db;

    public async Task<Result<SeatResponse>> Handle(DepositSeatCommand c, CancellationToken ct)
    {
        var seat = await _db.TourCustomers.FirstOrDefaultAsync(s => s.Id == c.SeatId, ct);
        if (seat is null)
        {
            return Error.NotFound();
        }

        seat.UpfrontAmount += c.Amount;
        seat.HoldExpiresAt = null;   // đã có tiền → không còn giữ-chỗ-đếm-ngược
        await _db.SaveChangesAsync(ct);
        return SeatMapper.ToSeatResponse(seat);
    }
}
