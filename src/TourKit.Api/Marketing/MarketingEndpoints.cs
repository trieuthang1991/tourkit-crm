using TourKit.Api.Application;
using TourKit.Api.Authz;
using TourKit.Api.Marketing.Features;
using TourKit.Shared.Application;

namespace TourKit.Api.Marketing;

/// <summary>REST endpoints cho chiến dịch marketing dưới /api/v1/marketing/campaigns.
/// Endpoint mỏng: map request → command/query → dispatch → map Result sang HTTP (conventions §6).
/// Gửi thật (Email/SMS/Zalo) nằm ngoài phạm vi — chỉ quản lý chiến dịch + ghi log gửi.</summary>
public static class MarketingEndpoints
{
    public static IEndpointRouteBuilder MapMarketingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/marketing/campaigns");

        group.MapGet("/", async (IDispatcher dispatcher, int? page, int? size, CancellationToken ct) =>
            (await dispatcher.Send(new ListCampaignsQuery(page ?? 1, size ?? 20), ct))
                .Match(p => Results.Ok(p))).RequireAuthorization(Permissions.MarketingView);

        group.MapGet("/{id:guid}", async (Guid id, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new GetCampaignQuery(id), ct))
                .Match(c => Results.Ok(c))).RequireAuthorization(Permissions.MarketingView);

        group.MapPost("/", async (CreateCampaignRequest body, IDispatcher dispatcher, CancellationToken ct) =>
        {
            var command = new CreateCampaignCommand(body.Name, body.Channel, body.Subject, body.Body);
            var result = await dispatcher.Send(command, ct);
            return result.Match(c => Results.Created($"/api/v1/marketing/campaigns/{c.Id}", c));
        }).RequireAuthorization(Permissions.MarketingCreate);

        group.MapPut("/{id:guid}", async (Guid id, UpdateCampaignRequest body, IDispatcher dispatcher, CancellationToken ct) =>
        {
            var command = new UpdateCampaignCommand(id, body.Name, body.Channel, body.Subject, body.Body, body.Status);
            var result = await dispatcher.Send(command, ct);
            return result.Match(_ => Results.NoContent());
        }).RequireAuthorization(Permissions.MarketingCreate);

        group.MapDelete("/{id:guid}", async (Guid id, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new DeleteCampaignCommand(id), ct))
                .Match(_ => Results.NoContent())).RequireAuthorization(Permissions.MarketingCreate);

        group.MapPost("/{id:guid}/send", async (Guid id, SendCampaignRequest body, IDispatcher dispatcher, CancellationToken ct) =>
        {
            var command = new SendCampaignCommand(id, body.Recipients);
            var result = await dispatcher.Send(command, ct);
            return result.Match(r => Results.Ok(r));
        }).RequireAuthorization(Permissions.MarketingSend);

        group.MapGet("/{id:guid}/logs", async (Guid id, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new ListSendLogsQuery(id), ct))
                .Match(l => Results.Ok(l))).RequireAuthorization(Permissions.MarketingView);

        return app;
    }
}
