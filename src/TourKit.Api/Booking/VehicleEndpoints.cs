using TourKit.Api.Application;
using TourKit.Api.Authz;
using TourKit.Api.Booking.Features;
using TourKit.Shared.Application;

namespace TourKit.Api.Booking;

/// <summary>
/// REST endpoints cho Vehicle (xe) dưới /api/v1/vehicles.
/// Endpoint mỏng: map request → command/query → dispatch → map Result sang HTTP (conventions §6).
/// </summary>
public static class VehicleEndpoints
{
    public static IEndpointRouteBuilder MapVehicleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/vehicles");

        group.MapGet("/", async (IDispatcher dispatcher, int? page, int? size, CancellationToken ct) =>
            (await dispatcher.Send(new ListVehiclesQuery(page ?? 1, size ?? 20), ct))
                .Match(p => Results.Ok(p))).RequireAuthorization(Permissions.VehicleView);

        group.MapPost("/", async (CreateVehicleRequest body, IDispatcher dispatcher, CancellationToken ct) =>
        {
            var command = new CreateVehicleCommand(body.Name, body.FirmName, body.SeatType, body.Status);
            var result = await dispatcher.Send(command, ct);
            return result.Match(v => Results.Created($"/api/v1/vehicles/{v.Id}", v));
        }).RequireAuthorization(Permissions.VehicleManage);

        group.MapPut("/{id:guid}", async (Guid id, UpdateVehicleRequest body, IDispatcher dispatcher, CancellationToken ct) =>
        {
            var command = new UpdateVehicleCommand(id, body.Name, body.FirmName, body.SeatType, body.Status);
            var result = await dispatcher.Send(command, ct);
            return result.Match(_ => Results.NoContent());
        }).RequireAuthorization(Permissions.VehicleManage);

        group.MapDelete("/{id:guid}", async (Guid id, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new DeleteVehicleCommand(id), ct))
                .Match(_ => Results.NoContent())).RequireAuthorization(Permissions.VehicleManage);

        return app;
    }
}
