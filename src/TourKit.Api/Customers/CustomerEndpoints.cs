using TourKit.Api.Application;
using TourKit.Api.Authz;
using TourKit.Api.Customers.Features;
using TourKit.Shared.Application;

namespace TourKit.Api.Customers;

/// <summary>
/// REST endpoints cho Customer dưới /api/v1/customers.
/// Endpoint mỏng: map request → command/query → dispatch → map Result sang HTTP (conventions §6).
/// </summary>
public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/customers");

        group.MapGet("/", async (IDispatcher dispatcher, int? page, int? size, CancellationToken ct) =>
            (await dispatcher.Send(new ListCustomersQuery(page ?? 1, size ?? 20), ct))
                .Match(p => Results.Ok(p))).RequireAuthorization(Permissions.CustomerView);

        group.MapGet("/{id:guid}", async (Guid id, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new GetCustomerQuery(id), ct))
                .Match(c => Results.Ok(c))).RequireAuthorization(Permissions.CustomerView);

        group.MapPost("/", async (CreateCustomerRequest body, IDispatcher dispatcher, CancellationToken ct) =>
        {
            var command = new CreateCustomerCommand(body.FullName, body.Phone);
            var result = await dispatcher.Send(command, ct);
            return result.Match(c => Results.Created($"/api/v1/customers/{c.Id}", c));
        }).RequireAuthorization(Permissions.CustomerCreate);

        group.MapPut("/{id:guid}", async (Guid id, UpdateCustomerRequest body, IDispatcher dispatcher, CancellationToken ct) =>
        {
            var command = new UpdateCustomerCommand(id, body.FullName, body.Phone);
            var result = await dispatcher.Send(command, ct);
            return result.Match(_ => Results.NoContent());
        }).RequireAuthorization(Permissions.CustomerUpdate);

        group.MapDelete("/{id:guid}", async (Guid id, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new DeleteCustomerCommand(id), ct))
                .Match(_ => Results.NoContent())).RequireAuthorization(Permissions.CustomerDelete);

        return app;
    }
}
