using Microsoft.EntityFrameworkCore;
using TourKit.Api.Authz;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;

namespace TourKit.Api.Catalog;

public sealed record CreateMarketTypeRequest(string Name, Guid? ParentId, int SortOrder);

public sealed record MarketTypeResponse(Guid Id, string Name, Guid? ParentId, int SortOrder, int Status);

public static class MarketTypeEndpoints
{
    public static IEndpointRouteBuilder MapMarketTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/market-types");

        group.MapGet("/", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.MarketTypes.AsNoTracking()
                .OrderBy(m => m.SortOrder)
                .Select(m => ToResponse(m)).ToListAsync(ct))).RequireAuthorization(Permissions.MarketView);

        group.MapPost("/", async (CreateMarketTypeRequest body, AppDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.Name))
            {
                return Validation("Name là bắt buộc.");
            }

            var m = new MarketType
            {
                Name = body.Name.Trim(),
                ParentId = body.ParentId,
                SortOrder = body.SortOrder,
            };
            db.MarketTypes.Add(m);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/market-types/{m.Id}", ToResponse(m));
        }).RequireAuthorization(Permissions.MarketManage);

        return app;
    }

    private static MarketTypeResponse ToResponse(MarketType m) => new(m.Id, m.Name, m.ParentId, m.SortOrder, m.Status);

    private static IResult Validation(string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { ["Request"] = [message] });
}
