using Microsoft.EntityFrameworkCore;
using TourKit.Application.Auth;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Entities;
using TourKit.Shared.Security;

namespace TourKit.Infrastructure.Auth;

public sealed class UserIdentityStore(AppDbContext db) : IUserIdentityStore
{
    public Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalizedEmail = UserEmail.Normalize(email);
        return db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(user => !user.IsDeleted && user.NormalizedEmail == normalizedEmail, ct);
    }

    public Task<User?> FindByIdAsync(Guid userId, CancellationToken ct = default)
        => db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(user => !user.IsDeleted && user.Id == userId, ct);

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
    {
        var normalizedEmail = UserEmail.Normalize(email);
        return db.Users.IgnoreQueryFilters()
            .AnyAsync(user => !user.IsDeleted && user.NormalizedEmail == normalizedEmail, ct);
    }

    public Task<bool> TenantIsActiveAsync(Guid tenantId, CancellationToken ct = default)
        => db.Tenants.AnyAsync(tenant => !tenant.IsDeleted && tenant.Id == tenantId, ct);

    public Task<UserExternalLogin?> FindExternalAsync(
        string provider,
        string subject,
        CancellationToken ct = default)
        => db.UserExternalLogins.IgnoreQueryFilters()
            .FirstOrDefaultAsync(login =>
                !login.IsDeleted && login.Provider == provider && login.ProviderSubject == subject, ct);

    public Task<bool> HasExternalAsync(Guid userId, string provider, CancellationToken ct = default)
        => db.UserExternalLogins.IgnoreQueryFilters()
            .AnyAsync(login => !login.IsDeleted && login.UserId == userId && login.Provider == provider, ct);

    public async Task AddExternalAsync(UserExternalLogin login, CancellationToken ct = default)
    {
        db.UserExternalLogins.Add(login);
        await db.SaveChangesAsync(ct);
    }
}
