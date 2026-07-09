using TourKit.Api.Application;
using TourKit.Api.Authz;
using TourKit.Api.Crm.Features;
using TourKit.Shared.Application;

namespace TourKit.Api.Crm;

/// <summary>
/// REST endpoints cho CustomerCare (chăm sóc khách hàng) dưới /api/v1/customer-cares.
/// Endpoint mỏng: map request → command/query → dispatch → map Result sang HTTP (conventions §6).
/// </summary>
public static class CustomerCareEndpoints
{
    public static IEndpointRouteBuilder MapCustomerCareEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/customer-cares");

        group.MapGet("/", async (IDispatcher dispatcher, int? page, int? size, CancellationToken ct) =>
            (await dispatcher.Send(new ListCustomerCaresQuery(page ?? 1, size ?? 20), ct))
                .Match(p => Results.Ok(p))).RequireAuthorization(Permissions.CareView);

        group.MapPost("/", async (CreateCustomerCareRequest body, IDispatcher dispatcher, CancellationToken ct) =>
        {
            var command = new CreateCustomerCareCommand(
                body.CustomerId, body.Title, body.Detail, body.RemindAt, body.AssignedToUserId, body.Status);
            var result = await dispatcher.Send(command, ct);
            return result.Match(r => Results.Created($"/api/v1/customer-cares/{r.Id}", r));
        }).RequireAuthorization(Permissions.CareManage);

        group.MapPut("/{id:guid}", async (Guid id, UpdateCustomerCareRequest body, IDispatcher dispatcher, CancellationToken ct) =>
        {
            var command = new UpdateCustomerCareCommand(
                id, body.Title, body.Detail, body.RemindAt, body.Feedback, body.AssignedToUserId, body.Status);
            var result = await dispatcher.Send(command, ct);
            return result.Match(_ => Results.NoContent());
        }).RequireAuthorization(Permissions.CareManage);

        group.MapDelete("/{id:guid}", async (Guid id, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new DeleteCustomerCareCommand(id), ct))
                .Match(_ => Results.NoContent())).RequireAuthorization(Permissions.CareManage);

        return app;
    }
}
