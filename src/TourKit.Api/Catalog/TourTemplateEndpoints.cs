using Microsoft.EntityFrameworkCore;
using TourKit.Api.Application;
using TourKit.Api.Authz;
using TourKit.Api.Catalog.Features;
using TourKit.Shared.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Application;

namespace TourKit.Api.Catalog;

public static class TourTemplateEndpoints
{
    public static IEndpointRouteBuilder MapTourTemplateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tour-templates");

        // Slice mẫu: qua dispatcher + phân trang (query ?page=&size=).
        group.MapGet("/", async (IDispatcher dispatcher, int? page, int? size, CancellationToken ct) =>
            (await dispatcher.Send(new ListTourTemplatesQuery(page ?? 1, size ?? 20), ct))
                .Match(p => Results.Ok(p))).RequireAuthorization(Permissions.TourView);

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var t = await db.TourTemplates.AsNoTracking()
                .Where(x => x.Id == id).Select(x => ToResponse(x)).FirstOrDefaultAsync(ct);
            return t is null ? Results.NotFound() : Results.Ok(t);
        }).RequireAuthorization(Permissions.TourView);

        // Slice mẫu: endpoint MỎNG — chỉ map request → command → dispatch → map Result sang HTTP.
        group.MapPost("/", async (CreateTourTemplateRequest body, IDispatcher dispatcher, CancellationToken ct) =>
        {
            var command = new CreateTourTemplateCommand(
                body.Code, body.Title, body.TourType, body.TotalSlots, body.ReservationHours,
                body.PriceAdult, body.PriceChild, body.PriceChildSmall, body.PriceBaby, body.TermsNote);
            var result = await dispatcher.Send(command, ct);
            return result.Match(r => Results.Created($"/api/v1/tour-templates/{r.Id}", r));
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
