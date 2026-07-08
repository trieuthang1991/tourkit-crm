using Microsoft.EntityFrameworkCore;
using TourKit.Api.Authz;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;

namespace TourKit.Api.Crm;

/// <summary>REST endpoints cho Lead (phễu bán) dưới /api/v1/leads. Cô lập tenant + gác quyền lead.*.</summary>
public static class LeadEndpoints
{
    public static IEndpointRouteBuilder MapLeadEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/leads");

        group.MapGet("/", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.Leads.AsNoTracking()
                .OrderByDescending(l => l.CreatedAt)
                .Select(l => ToResponse(l)).ToListAsync(ct)))
            .RequireAuthorization(Permissions.LeadView);

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var lead = await db.Leads.AsNoTracking()
                .Where(l => l.Id == id).Select(l => ToResponse(l)).FirstOrDefaultAsync(ct);
            return lead is null ? Results.NotFound() : Results.Ok(lead);
        }).RequireAuthorization(Permissions.LeadView);

        group.MapPost("/", async (CreateLeadRequest body, AppDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.FullName))
            {
                return Validation("FullName là bắt buộc.");
            }

            var lead = new Lead
            {
                FullName = body.FullName.Trim(), Phone = body.Phone, Email = body.Email,
                Source = body.Source, AssignedToUserId = body.AssignedToUserId,
            };
            db.Leads.Add(lead);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/leads/{lead.Id}", ToResponse(lead));
        }).RequireAuthorization(Permissions.LeadCreate);

        group.MapPut("/{id:guid}", async (Guid id, UpdateLeadRequest body, AppDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.FullName))
            {
                return Validation("FullName là bắt buộc.");
            }

            var lead = await db.Leads.FirstOrDefaultAsync(l => l.Id == id, ct);
            if (lead is null)
            {
                return Results.NotFound();
            }

            lead.FullName = body.FullName.Trim();
            lead.Phone = body.Phone;
            lead.Email = body.Email;
            lead.Source = body.Source;
            lead.Status = body.Status;
            lead.AssignedToUserId = body.AssignedToUserId;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.LeadUpdate);

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var lead = await db.Leads.FirstOrDefaultAsync(l => l.Id == id, ct);
            if (lead is null)
            {
                return Results.NotFound();
            }

            lead.IsDeleted = true;   // soft delete
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.LeadDelete);

        group.MapPost("/{id:guid}/convert", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var lead = await db.Leads.FirstOrDefaultAsync(l => l.Id == id, ct);
            if (lead is null)
            {
                return Results.NotFound();
            }

            if (lead.ConvertedCustomerId is not null)
            {
                return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Lead đã được convert.");
            }

            var customer = new Customer { FullName = lead.FullName, Phone = lead.Phone };
            db.Customers.Add(customer);
            lead.Status = LeadStatus.Won;
            lead.ConvertedCustomerId = customer.Id;
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/customers/{customer.Id}", new ConvertLeadResponse(customer.Id));
        }).RequireAuthorization(Permissions.LeadConvert);

        return app;
    }

    private static LeadResponse ToResponse(Lead l) => new(
        l.Id, l.FullName, l.Phone, l.Email, l.Source, l.Status, l.AssignedToUserId, l.ConvertedCustomerId);

    private static IResult Validation(string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { ["Request"] = [message] });
}
