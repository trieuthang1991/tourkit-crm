using TourKit.Api.Application;
using TourKit.Api.Authz;
using TourKit.Api.Catalog.Features;
using TourKit.Shared.Application;

namespace TourKit.Api.Catalog;

/// <summary>Loại thị trường (legacy MarketType). Endpoint mỏng: dispatch → map Result sang HTTP.</summary>
public static class MarketTypeEndpoints
{
    public static IEndpointRouteBuilder MapMarketTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/market-types");

        group.MapGet("/", async (IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new ListMarketTypesQuery(), ct))
                .Match(list => Results.Ok(list))).RequireAuthorization(Permissions.MarketView);

        group.MapPost("/", async (CreateMarketTypeCommand body, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(body, ct))
                .Match(m => Results.Created($"/api/v1/market-types/{m.Id}", m))).RequireAuthorization(Permissions.MarketManage);

        group.MapPut("/{id:guid}", async (Guid id, UpdateMarketTypeBody body, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new UpdateMarketTypeCommand(id, body.Name, body.ParentId, body.SortOrder), ct))
                .Match(_ => Results.NoContent())).RequireAuthorization(Permissions.MarketManage);

        group.MapDelete("/{id:guid}", async (Guid id, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new DeleteMarketTypeCommand(id), ct))
                .Match(_ => Results.NoContent())).RequireAuthorization(Permissions.MarketManage);

        return app;
    }
}

public sealed record UpdateMarketTypeBody(string Name, Guid? ParentId, int SortOrder);
