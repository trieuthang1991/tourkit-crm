using TourKit.Api.Application;
using TourKit.Api.Authz;
using TourKit.Api.Providers.Features;
using TourKit.Shared.Application;

namespace TourKit.Api.Providers;

/// <summary>
/// REST endpoints cho ServiceItem (danh mục dịch vụ) dưới /api/v1/service-items.
/// Endpoint mỏng: map request → command/query → dispatch → map Result sang HTTP (conventions §6).
/// </summary>
public static class ServiceItemEndpoints
{
    public static IEndpointRouteBuilder MapServiceItemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/service-items");

        group.MapGet("/", async (IDispatcher dispatcher, int? page, int? size, CancellationToken ct) =>
            (await dispatcher.Send(new ListServiceItemsQuery(page ?? 1, size ?? 20), ct))
                .Match(p => Results.Ok(p))).RequireAuthorization(Permissions.ServiceView);

        group.MapPost("/", async (CreateServiceItemRequest body, IDispatcher dispatcher, CancellationToken ct) =>
        {
            var command = new CreateServiceItemCommand(body.Code, body.Name, body.Category, body.Status);
            var result = await dispatcher.Send(command, ct);
            return result.Match(p => Results.Created($"/api/v1/service-items/{p.Id}", p));
        }).RequireAuthorization(Permissions.ServiceManage);

        group.MapPut("/{id:guid}", async (Guid id, UpdateServiceItemRequest body, IDispatcher dispatcher, CancellationToken ct) =>
        {
            var command = new UpdateServiceItemCommand(id, body.Name, body.Category, body.Status);
            var result = await dispatcher.Send(command, ct);
            return result.Match(_ => Results.NoContent());
        }).RequireAuthorization(Permissions.ServiceManage);

        group.MapDelete("/{id:guid}", async (Guid id, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new DeleteServiceItemCommand(id), ct))
                .Match(_ => Results.NoContent())).RequireAuthorization(Permissions.ServiceManage);

        return app;
    }
}
