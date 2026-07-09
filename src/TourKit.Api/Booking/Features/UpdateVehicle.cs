using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Booking.Features;

public sealed record UpdateVehicleCommand(Guid Id, string Name, string? FirmName, int SeatType, int Status) : ICommand<bool>;

public sealed class UpdateVehicleValidator : AbstractValidator<UpdateVehicleCommand>
{
    public UpdateVehicleValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
    }
}

public sealed class UpdateVehicleHandler : ICommandHandler<UpdateVehicleCommand, bool>
{
    private readonly AppDbContext _db;

    public UpdateVehicleHandler(AppDbContext db) => _db = db;

    public async Task<Result<bool>> Handle(UpdateVehicleCommand c, CancellationToken ct)
    {
        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(x => x.Id == c.Id, ct);
        if (vehicle is null)
        {
            return Error.NotFound();
        }

        vehicle.Name = c.Name.Trim();
        vehicle.FirmName = c.FirmName;
        vehicle.SeatType = c.SeatType;
        vehicle.Status = c.Status;
        await _db.SaveChangesAsync(ct);

        return true;
    }
}
