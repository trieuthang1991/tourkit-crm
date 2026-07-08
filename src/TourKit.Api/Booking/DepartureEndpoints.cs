using Microsoft.EntityFrameworkCore;
using TourKit.Api.Authz;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;

namespace TourKit.Api.Booking;

/// <summary>Mở/liệt kê/xem chuyến khởi hành (TourDeparture) dưới /api/v1/tour-departures.</summary>
public static class DepartureEndpoints
{
    public static IEndpointRouteBuilder MapDepartureEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tour-departures");

        group.MapGet("/", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.TourDepartures.AsNoTracking()
                .OrderBy(d => d.DepartureDate)
                .Select(d => ToResponse(d)).ToListAsync(ct)))
            .RequireAuthorization(Permissions.DepartureView);

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var d = await db.TourDepartures.AsNoTracking()
                .Where(x => x.Id == id).Select(x => ToResponse(x)).FirstOrDefaultAsync(ct);
            return d is null ? Results.NotFound() : Results.Ok(d);
        }).RequireAuthorization(Permissions.DepartureView);

        group.MapPost("/", async (CreateDepartureRequest body, AppDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.Code) || string.IsNullOrWhiteSpace(body.Title))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["Request"] = ["Code và Title là bắt buộc."],
                });
            }

            var departure = new TourDeparture
            {
                Code = body.Code.Trim(), Title = body.Title.Trim(), ParentTourId = body.TemplateId,
                DepartureDate = body.DepartureDate, EndDate = body.EndDate, TotalSlots = body.TotalSlots,
            };
            db.TourDepartures.Add(departure);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/tour-departures/{departure.Id}", ToResponse(departure));
        }).RequireAuthorization(Permissions.DepartureCreate);

        return app;
    }

    private static DepartureResponse ToResponse(TourDeparture d) => new(
        d.Id, d.Code, d.Title, d.ParentTourId, d.DepartureDate, d.EndDate, d.TotalSlots, d.Status);
}
