using TourKit.Api.Application;
using TourKit.Api.Authz;
using TourKit.Api.Commission.Features;
using TourKit.Shared.Application;

namespace TourKit.Api.Commission;

/// <summary>
/// REST endpoints cho CommissionRule dưới /api/v1/commission-rules.
/// Endpoint mỏng: map request → command/query → dispatch → map Result sang HTTP (conventions §6).
/// </summary>
public static class CommissionRuleEndpoints
{
    public static IEndpointRouteBuilder MapCommissionRuleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/commission-rules");

        group.MapGet("/", async (IDispatcher dispatcher, int? page, int? size, CancellationToken ct) =>
            (await dispatcher.Send(new ListCommissionRulesQuery(page ?? 1, size ?? 20), ct))
                .Match(p => Results.Ok(p))).RequireAuthorization(Permissions.CommissionView);

        group.MapPost("/", async (CreateCommissionRuleRequest body, IDispatcher dispatcher, CancellationToken ct) =>
        {
            var command = new CreateCommissionRuleCommand(body.UserId, body.Percentage, body.Status);
            var result = await dispatcher.Send(command, ct);
            return result.Match(r => Results.Created($"/api/v1/commission-rules/{r.Id}", r));
        }).RequireAuthorization(Permissions.CommissionCreate);

        group.MapPut("/{id:guid}", async (Guid id, UpdateCommissionRuleRequest body, IDispatcher dispatcher, CancellationToken ct) =>
        {
            var command = new UpdateCommissionRuleCommand(id, body.Percentage, body.Status);
            var result = await dispatcher.Send(command, ct);
            return result.Match(_ => Results.NoContent());
        }).RequireAuthorization(Permissions.CommissionCreate);

        group.MapDelete("/{id:guid}", async (Guid id, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new DeleteCommissionRuleCommand(id), ct))
                .Match(_ => Results.NoContent())).RequireAuthorization(Permissions.CommissionCreate);

        return app;
    }
}
