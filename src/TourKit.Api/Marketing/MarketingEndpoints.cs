using Microsoft.EntityFrameworkCore;
using TourKit.Api.Authz;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;

namespace TourKit.Api.Marketing;

/// <summary>REST endpoints cho chiến dịch marketing dưới /api/v1/marketing/campaigns.
/// Gửi thật (Email/SMS/Zalo) nằm ngoài phạm vi — chỉ quản lý chiến dịch + ghi log gửi.</summary>
public static class MarketingEndpoints
{
    public static IEndpointRouteBuilder MapMarketingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/marketing/campaigns");

        group.MapGet("/", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.MarketingCampaigns.AsNoTracking()
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => ToResponse(c)).ToListAsync(ct)))
            .RequireAuthorization(Permissions.MarketingView);

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var campaign = await db.MarketingCampaigns.AsNoTracking()
                .Where(c => c.Id == id).Select(c => ToResponse(c)).FirstOrDefaultAsync(ct);
            return campaign is null ? Results.NotFound() : Results.Ok(campaign);
        }).RequireAuthorization(Permissions.MarketingView);

        group.MapPost("/", async (CreateCampaignRequest body, AppDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.Name) || string.IsNullOrWhiteSpace(body.Body))
            {
                return Validation("Name và Body là bắt buộc.");
            }

            var campaign = new MarketingCampaign
            {
                Name = body.Name.Trim(), Channel = body.Channel,
                Subject = body.Subject, Body = body.Body,
            };
            db.MarketingCampaigns.Add(campaign);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/marketing/campaigns/{campaign.Id}", ToResponse(campaign));
        }).RequireAuthorization(Permissions.MarketingCreate);

        group.MapPut("/{id:guid}", async (Guid id, UpdateCampaignRequest body, AppDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.Name) || string.IsNullOrWhiteSpace(body.Body))
            {
                return Validation("Name và Body là bắt buộc.");
            }

            var campaign = await db.MarketingCampaigns.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (campaign is null)
            {
                return Results.NotFound();
            }

            campaign.Name = body.Name.Trim();
            campaign.Channel = body.Channel;
            campaign.Subject = body.Subject;
            campaign.Body = body.Body;
            campaign.Status = body.Status;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.MarketingCreate);

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var campaign = await db.MarketingCampaigns.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (campaign is null)
            {
                return Results.NotFound();
            }

            campaign.IsDeleted = true;   // soft delete
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.MarketingCreate);

        group.MapPost("/{id:guid}/send", async (Guid id, SendCampaignRequest body, AppDbContext db, CancellationToken ct) =>
        {
            var campaign = await db.MarketingCampaigns.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (campaign is null)
            {
                return Results.NotFound();
            }

            var now = DateTimeOffset.UtcNow;
            foreach (var recipient in body.Recipients)
            {
                // Chưa tích hợp gửi thật (Email/SMS/Zalo provider) — chỉ ghi log; follow-up.
                db.MarketingSendLogs.Add(new MarketingSendLog
                {
                    CampaignId = id, Recipient = recipient, Status = 1 /* sent-simulated */, SentAt = now,
                });
            }

            campaign.Status = 1;   // đã gửi
            await db.SaveChangesAsync(ct);
            return Results.Ok(new SendResultResponse(body.Recipients.Length));
        }).RequireAuthorization(Permissions.MarketingSend);

        group.MapGet("/{id:guid}/logs", async (Guid id, AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.MarketingSendLogs.AsNoTracking()
                .Where(l => l.CampaignId == id)
                .OrderByDescending(l => l.SentAt)
                .Select(l => new SendLogResponse(l.Id, l.Recipient, l.Status, l.SentAt))
                .ToListAsync(ct)))
            .RequireAuthorization(Permissions.MarketingView);

        return app;
    }

    private static CampaignResponse ToResponse(MarketingCampaign c) => new(
        c.Id, c.Name, c.Channel, c.Subject, c.Body, c.Status);

    private static IResult Validation(string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { ["Request"] = [message] });
}
