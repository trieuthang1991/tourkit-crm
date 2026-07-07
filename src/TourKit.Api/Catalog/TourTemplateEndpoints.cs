using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;

namespace TourKit.Api.Catalog;

public static class TourTemplateEndpoints
{
    public static IEndpointRouteBuilder MapTourTemplateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tour-templates").RequireAuthorization();

        group.MapGet("/", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.TourTemplates.AsNoTracking()
                .OrderBy(t => t.Title)
                .Select(t => ToResponse(t)).ToListAsync(ct)));

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var t = await db.TourTemplates.AsNoTracking()
                .Where(x => x.Id == id).Select(x => ToResponse(x)).FirstOrDefaultAsync(ct);
            return t is null ? Results.NotFound() : Results.Ok(t);
        });

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
        });

        return app;
    }

    // Projection dùng chung — LINQ dịch được vì chỉ gán thuộc tính.
    private static TourTemplateResponse ToResponse(TourTemplate t) => new(
        t.Id, t.Code, t.Title, t.TourType, t.TotalSlots, t.ReservationHours,
        t.PriceAdult, t.PriceChild, t.PriceChildSmall, t.PriceBaby, t.TermsNote, t.Status);

    private static IResult Validation(string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { ["Request"] = [message] });
}
