using TourKit.Api.Application;
using TourKit.Api.Authz;
using TourKit.Api.Crm.Features;
using TourKit.Shared.Application;

namespace TourKit.Api.Crm;

/// <summary>
/// REST endpoints cho TourRating (đánh giá sau tour) dưới /api/v1/tour-ratings.
/// Endpoint mỏng: map request → command/query → dispatch → map Result sang HTTP (conventions §6).
/// </summary>
public static class TourRatingEndpoints
{
    public static IEndpointRouteBuilder MapTourRatingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tour-ratings");

        group.MapGet("/", async (IDispatcher dispatcher, int? page, int? size, CancellationToken ct) =>
            (await dispatcher.Send(new ListTourRatingsQuery(page ?? 1, size ?? 20), ct))
                .Match(p => Results.Ok(p))).RequireAuthorization(Permissions.RatingView);

        group.MapPost("/", async (CreateTourRatingRequest body, IDispatcher dispatcher, CancellationToken ct) =>
        {
            var command = new CreateTourRatingCommand(
                body.TourDepartureId, body.OrderId, body.CustomerName, body.CustomerPhone, body.Stars, body.Comment, body.Status);
            var result = await dispatcher.Send(command, ct);
            return result.Match(r => Results.Created($"/api/v1/tour-ratings/{r.Id}", r));
        }).RequireAuthorization(Permissions.RatingManage);

        group.MapPut("/{id:guid}", async (Guid id, UpdateTourRatingRequest body, IDispatcher dispatcher, CancellationToken ct) =>
        {
            var command = new UpdateTourRatingCommand(
                id, body.CustomerName, body.CustomerPhone, body.Stars, body.Comment, body.Status);
            var result = await dispatcher.Send(command, ct);
            return result.Match(_ => Results.NoContent());
        }).RequireAuthorization(Permissions.RatingManage);

        group.MapDelete("/{id:guid}", async (Guid id, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new DeleteTourRatingCommand(id), ct))
                .Match(_ => Results.NoContent())).RequireAuthorization(Permissions.RatingManage);

        return app;
    }
}
