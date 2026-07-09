using Microsoft.EntityFrameworkCore;
using TourKit.Api.Authz;
using TourKit.Shared.Entities;
using TourKit.Infrastructure.Persistence;

namespace TourKit.Api.Catalog;

public sealed record AssigneeRequest(Guid UserId, AssigneeRole Role);

public sealed record AssigneeResponse(Guid Id, Guid UserId, AssigneeRole Role);

public static class TourAssigneeEndpoints
{
    public static IEndpointRouteBuilder MapTourAssigneeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tours/{tourId:guid}/assignees");

        group.MapGet("/", async (Guid tourId, AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.TourAssignees.AsNoTracking()
                .Where(a => a.TourId == tourId)
                .Select(a => new AssigneeResponse(a.Id, a.UserId, a.Role))
                .ToListAsync(ct))).RequireAuthorization(Permissions.TourView);

        group.MapPut("/", async (Guid tourId, AssigneeRequest[] body, AppDbContext db, CancellationToken ct) =>
        {
            var exists = await db.Tours.AnyAsync(t => t.Id == tourId, ct);
            if (!exists)
            {
                return Results.NotFound();
            }

            var old = await db.TourAssignees.Where(a => a.TourId == tourId).ToListAsync(ct);
            db.TourAssignees.RemoveRange(old);
            foreach (var a in body)
            {
                db.TourAssignees.Add(new TourAssignee { TourId = tourId, UserId = a.UserId, Role = a.Role });
            }

            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.TourUpdate);

        return app;
    }
}
