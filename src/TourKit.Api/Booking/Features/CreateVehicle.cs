using FluentValidation;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Booking.Features;

public sealed record CreateVehicleCommand(string Name, string? FirmName, int SeatType, int Status) : ICommand<VehicleResponse>;

public sealed class CreateVehicleValidator : AbstractValidator<CreateVehicleCommand>
{
    public CreateVehicleValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
    }
}

public sealed class CreateVehicleHandler : ICommandHandler<CreateVehicleCommand, VehicleResponse>
{
    private readonly AppDbContext _db;

    public CreateVehicleHandler(AppDbContext db) => _db = db;

    public async Task<Result<VehicleResponse>> Handle(CreateVehicleCommand c, CancellationToken ct)
    {
        var vehicle = new Vehicle
        {
            Name = c.Name.Trim(),
            FirmName = c.FirmName,
            SeatType = c.SeatType,
            Status = c.Status,
        };
        _db.Vehicles.Add(vehicle);
        await _db.SaveChangesAsync(ct);

        return new VehicleResponse(vehicle.Id, vehicle.Name, vehicle.FirmName, vehicle.SeatType, vehicle.Status);
    }
}
