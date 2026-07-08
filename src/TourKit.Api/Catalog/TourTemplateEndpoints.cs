using Microsoft.EntityFrameworkCore;
using TourKit.Api.Authz;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;

namespace TourKit.Api.Catalog;

public static class TourTemplateEndpoints
{
    public static IEndpointRouteBuilder MapTourTemplateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tour-templates");

        group.MapGet("/", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.TourTemplates.AsNoTracking()
                .OrderBy(t => t.Title)
                .Select(t => ToResponse(t)).ToListAsync(ct))).RequireAuthorization(Permissions.TourView);

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var t = await db.TourTemplates.AsNoTracking()
                .Where(x => x.Id == id).Select(x => ToResponse(x)).FirstOrDefaultAsync(ct);
            return t is null ? Results.NotFound() : Results.Ok(t);
        }).RequireAuthorization(Permissions.TourView);

        group.MapPost("/", async (CreateTourTemplateRequest body, AppDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.Code) || string.IsNullOrWhiteSpace(body.Title))
            {
                return Validation("Code và Title là bắt buộc.");
            }

            var t = new TourTemplate
            {
                Code = body.Code.Trim(), Title = body.Title.Trim(), TourType = body.TourType,
                TotalSlots = body.TotalSlots, ReservationHours = body.ReservationHours,
                PriceAdult = body.PriceAdult, PriceChild = body.PriceChild,
                PriceChildSmall = body.PriceChildSmall, PriceBaby = body.PriceBaby,
                TermsNote = body.TermsNote,
            };
            db.TourTemplates.Add(t);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/tour-templates/{t.Id}", ToResponse(t));
        }).RequireAuthorization(Permissions.TourCreate);

        group.MapPut("/{id:guid}", async (Guid id, UpdateTourTemplateRequest body, AppDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.Title))
            {
                return Validation("Title là bắt buộc.");
            }

            var t = await db.TourTemplates.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (t is null)
            {
                return Results.NotFound();
            }

            t.Title = body.Title.Trim();
            t.TourType = body.TourType;
            t.TotalSlots = body.TotalSlots;
            t.ReservationHours = body.ReservationHours;
            t.PriceAdult = body.PriceAdult;
            t.PriceChild = body.PriceChild;
            t.PriceChildSmall = body.PriceChildSmall;
            t.PriceBaby = body.PriceBaby;
            t.TermsNote = body.TermsNote;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.TourUpdate);

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var t = await db.TourTemplates.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (t is null)
            {
                return Results.NotFound();
            }

            t.IsDeleted = true;   // soft delete (conventions §5)
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.TourDelete);

        group.MapGet("/{id:guid}/itinerary", async (Guid id, AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.TourItineraries.AsNoTracking()
                .Where(i => i.TourId == id).OrderBy(i => i.DayIndex)
                .Select(i => new ItineraryDayResponse(i.Id, i.DayIndex, i.Title, i.Detail))
                .ToListAsync(ct))).RequireAuthorization(Permissions.TourView);

        group.MapPut("/{id:guid}/itinerary", async (Guid id, ItineraryDayRequest[] body, AppDbContext db, CancellationToken ct) =>
        {
            var exists = await db.TourTemplates.AnyAsync(x => x.Id == id, ct);
            if (!exists)
            {
                return Results.NotFound();
            }

            var old = await db.TourItineraries.Where(i => i.TourId == id).ToListAsync(ct);
            db.TourItineraries.RemoveRange(old);   // hard-remove dòng lịch cũ rồi ghi lại (thay toàn bộ)
            foreach (var day in body)
            {
                db.TourItineraries.Add(new TourItinerary
                {
                    TourId = id, DayIndex = day.DayIndex, Title = day.Title.Trim(), Detail = day.Detail,
                });
            }

            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.TourUpdate);

        group.MapGet("/{id:guid}/price-scenarios", async (Guid id, AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.PriceScenarios.AsNoTracking()
                .Where(p => p.TourTemplateId == id).OrderBy(p => p.FromQty)
                .Select(p => new PriceScenarioResponse(p.Id, p.FromQty, p.ToQty, p.UnitPrice))
                .ToListAsync(ct))).RequireAuthorization(Permissions.TourView);

        group.MapPut("/{id:guid}/price-scenarios", async (Guid id, PriceScenarioRequest[] body, AppDbContext db, CancellationToken ct) =>
        {
            var exists = await db.TourTemplates.AnyAsync(x => x.Id == id, ct);
            if (!exists)
            {
                return Results.NotFound();
            }

            var old = await db.PriceScenarios.Where(p => p.TourTemplateId == id).ToListAsync(ct);
            db.PriceScenarios.RemoveRange(old);   // thay toàn bộ bảng giá cỡ đoàn (giống itinerary)
            foreach (var scenario in body)
            {
                db.PriceScenarios.Add(new PriceScenario
                {
                    TourTemplateId = id, FromQty = scenario.FromQty, ToQty = scenario.ToQty, UnitPrice = scenario.UnitPrice,
                });
            }

            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.TourUpdate);

        return app;
    }

    // Projection dùng chung — LINQ dịch được vì chỉ gán thuộc tính.
    private static TourTemplateResponse ToResponse(TourTemplate t) => new(
        t.Id, t.Code, t.Title, t.TourType, t.TotalSlots, t.ReservationHours,
        t.PriceAdult, t.PriceChild, t.PriceChildSmall, t.PriceBaby, t.TermsNote, t.Status);

    private static IResult Validation(string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { ["Request"] = [message] });
}
