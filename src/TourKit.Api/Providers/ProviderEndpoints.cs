using TourKit.Api.Application;
using TourKit.Api.Authz;
using TourKit.Api.Providers.Features;
using TourKit.Shared.Application;

namespace TourKit.Api.Providers;

/// <summary>
/// REST endpoints cho Provider (nhà cung cấp) dưới /api/v1/providers.
/// Endpoint mỏng: map request → command/query → dispatch → map Result sang HTTP (conventions §6).
/// </summary>
public static class ProviderEndpoints
{
    public static IEndpointRouteBuilder MapProviderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/providers");

        group.MapGet("/", async (IDispatcher dispatcher, int? page, int? size, CancellationToken ct) =>
            (await dispatcher.Send(new ListProvidersQuery(page ?? 1, size ?? 20), ct))
                .Match(p => Results.Ok(p))).RequireAuthorization(Permissions.ProviderView);

        group.MapGet("/{id:guid}", async (Guid id, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new GetProviderQuery(id), ct))
                .Match(p => Results.Ok(p))).RequireAuthorization(Permissions.ProviderView);

        group.MapPost("/", async (CreateProviderRequest body, IDispatcher dispatcher, CancellationToken ct) =>
        {
            var command = new CreateProviderCommand(
                body.Code, body.Name, body.Type, body.Phone, body.Email, body.Address,
                body.TaxCode, body.ContactPerson, body.BankAccount, body.BankName, body.Rate, body.Status);
            var result = await dispatcher.Send(command, ct);
            return result.Match(p => Results.Created($"/api/v1/providers/{p.Id}", p));
        }).RequireAuthorization(Permissions.ProviderCreate);

        group.MapPut("/{id:guid}", async (Guid id, UpdateProviderRequest body, IDispatcher dispatcher, CancellationToken ct) =>
        {
            var command = new UpdateProviderCommand(
                id, body.Name, body.Type, body.Phone, body.Email, body.Address,
                body.TaxCode, body.ContactPerson, body.BankAccount, body.BankName, body.Rate, body.Status);
            var result = await dispatcher.Send(command, ct);
            return result.Match(_ => Results.NoContent());
        }).RequireAuthorization(Permissions.ProviderUpdate);

        group.MapDelete("/{id:guid}", async (Guid id, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new DeleteProviderCommand(id), ct))
                .Match(_ => Results.NoContent())).RequireAuthorization(Permissions.ProviderDelete);

        return app;
    }
}
