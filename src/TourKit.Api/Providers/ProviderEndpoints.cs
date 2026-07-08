using Microsoft.EntityFrameworkCore;
using TourKit.Api.Authz;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;

namespace TourKit.Api.Providers;

/// <summary>
/// REST endpoints cho Provider (nhà cung cấp) dưới /api/v1/providers.
/// Endpoint mỏng: validate → thao tác DbContext → map DTO (conventions §6).
/// </summary>
public static class ProviderEndpoints
{
    public static IEndpointRouteBuilder MapProviderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/providers");

        group.MapGet("/", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.Providers.AsNoTracking()
                .OrderBy(p => p.Name)
                .Select(p => ToResponse(p))
                .ToListAsync(ct))).RequireAuthorization(Permissions.ProviderView);

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var provider = await db.Providers.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
            return provider is null ? Results.NotFound() : Results.Ok(ToResponse(provider));
        }).RequireAuthorization(Permissions.ProviderView);

        group.MapPost("/", async (CreateProviderRequest body, AppDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.Code) || string.IsNullOrWhiteSpace(body.Name))
            {
                return ValidationError("Code và Name là bắt buộc.");
            }

            var provider = new Provider
            {
                Code = body.Code.Trim(),
                Name = body.Name.Trim(),
                Type = body.Type,
                Phone = body.Phone,
                Email = body.Email,
                Address = body.Address,
                TaxCode = body.TaxCode,
                ContactPerson = body.ContactPerson,
                BankAccount = body.BankAccount,
                BankName = body.BankName,
                Rate = body.Rate,
                Status = body.Status,
            };
            db.Providers.Add(provider);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/v1/providers/{provider.Id}", ToResponse(provider));
        }).RequireAuthorization(Permissions.ProviderCreate);

        group.MapPut("/{id:guid}", async (Guid id, UpdateProviderRequest body, AppDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.Name))
            {
                return ValidationError("Name là bắt buộc.");
            }

            var provider = await db.Providers.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (provider is null)
            {
                return Results.NotFound();
            }

            provider.Name = body.Name.Trim();
            provider.Type = body.Type;
            provider.Phone = body.Phone;
            provider.Email = body.Email;
            provider.Address = body.Address;
            provider.TaxCode = body.TaxCode;
            provider.ContactPerson = body.ContactPerson;
            provider.BankAccount = body.BankAccount;
            provider.BankName = body.BankName;
            provider.Rate = body.Rate;
            provider.Status = body.Status;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.ProviderUpdate);

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var provider = await db.Providers.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (provider is null)
            {
                return Results.NotFound();
            }

            provider.IsDeleted = true; // soft delete (conventions §5)
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireAuthorization(Permissions.ProviderDelete);

        return app;
    }

    private static ProviderResponse ToResponse(Provider p) => new(
        p.Id, p.Code, p.Name, p.Type, p.Phone, p.Email, p.Address,
        p.TaxCode, p.ContactPerson, p.BankAccount, p.BankName, p.Rate, p.Status);

    // Validation tối thiểu cho foundation; Phase sau thay bằng FluentValidation (conventions §6).
    private static IResult ValidationError(string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { ["Request"] = [message] });
}
