using TourKit.Api.Application;
using TourKit.Api.Authz;
using TourKit.Api.Crm.Features;
using TourKit.Shared.Application;

namespace TourKit.Api.Crm;

/// <summary>
/// REST endpoints cho Lead (phễu bán) dưới /api/v1/leads. Cô lập tenant + gác quyền lead.*.
/// Endpoint mỏng: map request → command/query → dispatch → map Result sang HTTP (conventions §6).
/// </summary>
public static class LeadEndpoints
{
    public static IEndpointRouteBuilder MapLeadEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/leads");

        group.MapGet("/", async (IDispatcher dispatcher, int? page, int? size, CancellationToken ct) =>
            (await dispatcher.Send(new ListLeadsQuery(page ?? 1, size ?? 20), ct))
                .Match(p => Results.Ok(p))).RequireAuthorization(Permissions.LeadView);

        group.MapGet("/{id:guid}", async (Guid id, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new GetLeadQuery(id), ct))
                .Match(l => Results.Ok(l))).RequireAuthorization(Permissions.LeadView);

        group.MapPost("/", async (CreateLeadRequest body, IDispatcher dispatcher, CancellationToken ct) =>
        {
            var command = new CreateLeadCommand(body.FullName, body.Phone, body.Email, body.Source, body.AssignedToUserId);
            var result = await dispatcher.Send(command, ct);
            return result.Match(l => Results.Created($"/api/v1/leads/{l.Id}", l));
        }).RequireAuthorization(Permissions.LeadCreate);

        group.MapPut("/{id:guid}", async (Guid id, UpdateLeadRequest body, IDispatcher dispatcher, CancellationToken ct) =>
        {
            var command = new UpdateLeadCommand(
                id, body.FullName, body.Phone, body.Email, body.Source, body.Status, body.AssignedToUserId);
            var result = await dispatcher.Send(command, ct);
            return result.Match(_ => Results.NoContent());
        }).RequireAuthorization(Permissions.LeadUpdate);

        group.MapDelete("/{id:guid}", async (Guid id, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new DeleteLeadCommand(id), ct))
                .Match(_ => Results.NoContent())).RequireAuthorization(Permissions.LeadDelete);

        group.MapPost("/{id:guid}/convert", async (Guid id, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new ConvertLeadCommand(id), ct))
                .Match(r => Results.Created($"/api/v1/customers/{r.CustomerId}", r)))
            .RequireAuthorization(Permissions.LeadConvert);

        return app;
    }
}
