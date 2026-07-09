using TourKit.Api.Application;
using TourKit.Api.Authz;
using TourKit.Api.Providers.Features;
using TourKit.Shared.Application;

namespace TourKit.Api.Providers;

/// <summary>
/// REST endpoints cho ProviderService (bảng giá dịch vụ theo NCC) dưới /api/v1/provider-services.
/// Endpoint mỏng: map request → command/query → dispatch → map Result sang HTTP (conventions §6).
/// </summary>
public static class ProviderServiceEndpoints
{
    public static IEndpointRouteBuilder MapProviderServiceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/provider-services");

        group.MapGet("/", async (IDispatcher dispatcher, int? page, int? size, Guid? providerId, CancellationToken ct) =>
            (await dispatcher.Send(new ListProviderServicesQuery(page ?? 1, size ?? 20, providerId), ct))
                .Match(p => Results.Ok(p))).RequireAuthorization(Permissions.ServiceView);

        group.MapPost("/", async (CreateProviderServiceRequest body, IDispatcher dispatcher, CancellationToken ct) =>
        {
            var command = new CreateProviderServiceCommand(
                body.ProviderId, body.ServiceItemId, body.PriceName, body.ContractPrice, body.PublicPrice,
                body.AmountOfPeople, body.Note, body.Status);
            var result = await dispatcher.Send(command, ct);
            return result.Match(p => Results.Created($"/api/v1/provider-services/{p.Id}", p));
        }).RequireAuthorization(Permissions.ServiceManage);

        group.MapPut("/{id:guid}", async (Guid id, UpdateProviderServiceRequest body, IDispatcher dispatcher, CancellationToken ct) =>
        {
            var command = new UpdateProviderServiceCommand(
                id, body.ServiceItemId, body.PriceName, body.ContractPrice, body.PublicPrice,
                body.AmountOfPeople, body.Note, body.Status);
            var result = await dispatcher.Send(command, ct);
            return result.Match(_ => Results.NoContent());
        }).RequireAuthorization(Permissions.ServiceManage);

        group.MapDelete("/{id:guid}", async (Guid id, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new DeleteProviderServiceCommand(id), ct))
                .Match(_ => Results.NoContent())).RequireAuthorization(Permissions.ServiceManage);

        return app;
    }
}
