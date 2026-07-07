using Microsoft.EntityFrameworkCore;
using TourKit.Api.Authz;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;

namespace TourKit.Api.Customers;

/// <summary>
/// REST endpoints cho Customer dưới /api/v1/customers.
/// Endpoint mỏng: validate → thao tác DbContext → map DTO (conventions §6).
/// Cô lập tenant do AppDbContext tự lo (query filter + SaveChanges interceptor).
/// </summary>
public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/customers");

        group.MapGet("/", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.Customers.AsNoTracking()
                .OrderBy(c => c.FullName)
                .Select(c => new CustomerResponse(c.Id, c.FullName, c.Phone))
                .ToListAsync(ct))).RequireAuthorization(Permissions.CustomerView);

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var customer = await db.Customers.AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new CustomerResponse(c.Id, c.FullName, c.Phone))
                .FirstOrDefaultAsync(ct);
            return customer is null ? Results.NotFound() : Results.Ok(customer);
        }).RequireAuthorization(Permissions.CustomerView);

        group.MapPost("/", async (CreateCustomerRequest body, AppDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.FullName))
            {
                return ValidationError();
            }

            var customer = new Customer { FullName = body.FullName.Trim(), Phone = body.Phone };
            db.Customers.Add(customer);
            await db.SaveChangesAsync(ct);

            var response = new CustomerResponse(customer.Id, customer.FullName, customer.Phone);
            return Results.Created($"/api/v1/customers/{customer.Id}", response);
        }).RequireAuthorization(Permissions.CustomerCreate);

        group.MapPut("/{id:guid}", async (Guid id, UpdateCustomerRequest body, AppDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.FullName))
            {
                return ValidationError();
            }

            var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (customer is null)
            {
                return Results.NotFound();
            }

            customer.FullName = body.FullName.Trim();
            customer.Phone = body.Phone;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.CustomerUpdate);

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var customer = await db.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (customer is null)
            {
                return Results.NotFound();
            }

            customer.IsDeleted = true; // soft delete (conventions §5)
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.CustomerDelete);

        return app;
    }

    // Validation tối thiểu cho foundation; Phase sau thay bằng FluentValidation (conventions §6).
    private static IResult ValidationError() =>
        Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["FullName"] = ["FullName là bắt buộc."],
        });
}
