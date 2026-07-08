using TourKit.Api.Application;
using TourKit.Api.Authz;
using TourKit.Api.Booking.Features;
using TourKit.Shared.Application;

namespace TourKit.Api.Booking;

/// <summary>
/// Mở/liệt kê/xem chuyến khởi hành (TourDeparture) dưới /api/v1/tour-departures.
/// Endpoint mỏng: map request → command/query → dispatch → map Result sang HTTP (conventions §6).
/// </summary>
public static class DepartureEndpoints
{
    public static IEndpointRouteBuilder MapDepartureEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tour-departures");

        group.MapGet("/", async (IDispatcher dispatcher, int? page, int? size, CancellationToken ct) =>
            (await dispatcher.Send(new ListDeparturesQuery(page ?? 1, size ?? 20), ct))
                .Match(p => Results.Ok(p))).RequireAuthorization(Permissions.DepartureView);

        group.MapGet("/{id:guid}", async (Guid id, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new GetDepartureQuery(id), ct))
                .Match(d => Results.Ok(d))).RequireAuthorization(Permissions.DepartureView);

        group.MapPost("/", async (CreateDepartureRequest body, IDispatcher dispatcher, CancellationToken ct) =>
        {
            var command = new CreateDepartureCommand(
                body.TemplateId, body.Code, body.Title, body.DepartureDate, body.EndDate, body.TotalSlots);
            var result = await dispatcher.Send(command, ct);
            return result.Match(d => Results.Created($"/api/v1/tour-departures/{d.Id}", d));
        }).RequireAuthorization(Permissions.DepartureCreate);

        return app;
    }
}
