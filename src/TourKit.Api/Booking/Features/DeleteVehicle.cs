using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Booking.Features;

public sealed record DeleteVehicleCommand(Guid Id) : ICommand<bool>;

public sealed class DeleteVehicleHandler : ICommandHandler<DeleteVehicleCommand, bool>
{
    private readonly AppDbContext _db;

    public DeleteVehicleHandler(AppDbContext db) => _db = db;

    public async Task<Result<bool>> Handle(DeleteVehicleCommand c, CancellationToken ct)
    {
        var vehicle = await _db.Vehicles.FirstOrDefaultAsync(x => x.Id == c.Id, ct);
        if (vehicle is null)
        {
            return Error.NotFound();
        }

        vehicle.IsDeleted = true; // soft delete (conventions §5)
        await _db.SaveChangesAsync(ct);

        return true;
    }
}
