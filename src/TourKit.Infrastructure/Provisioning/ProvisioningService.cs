using Microsoft.EntityFrameworkCore;
using Npgsql;
using TourKit.Application.Auth;
using TourKit.Application.Billing;
using TourKit.Application.Provisioning;
using TourKit.Shared.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Infrastructure.Tenancy;

using TourKit.Shared.Enums;
using SqliteException = Microsoft.Data.Sqlite.SqliteException;
using SqlServerException = Microsoft.Data.SqlClient.SqlException;

namespace TourKit.Infrastructure.Provisioning;

/// <summary>
/// Tạo tenant mới + user admin + role "Admin" (đủ quyền) trong một lần đăng ký.
/// Toàn bộ aggregate dùng ID sinh phía client và được lưu bằng một SaveChanges để relational provider tự bọc transaction.
/// </summary>
public sealed class ProvisioningService : IProvisioningService
{
    private const string UserEmailIndex = "IX_Users_NormalizedEmail";
    private const string TenantSlugIndex = "IX_Tenants_Slug";

    private readonly AppDbContext _db;
    private readonly AmbientTenantContext _tenant;
    private readonly IPasswordHasher _hasher;
    private readonly IUserIdentityStore _identity;

    public ProvisioningService(
        AppDbContext db,
        AmbientTenantContext tenant,
        IPasswordHasher hasher,
        IUserIdentityStore identity)
    {
        _db = db;
        _tenant = tenant;
        _hasher = hasher;
        _identity = identity;
    }

    public async Task<RegistrationOutcome> RegisterAsync(RegisterTenantRequest req)
    {
        var slug = (req.Slug ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(req.CompanyName)
            || string.IsNullOrWhiteSpace(req.AdminEmail) || (req.AdminPassword?.Length ?? 0) < 8)
        {
            return new RegistrationOutcome(RegistrationError.Invalid, null);
        }

        if (await _db.Tenants.AnyAsync(t => t.Slug == slug && !t.IsDeleted))
        {
            return new RegistrationOutcome(RegistrationError.SlugTaken, null);
        }

        if (await _identity.EmailExistsAsync(req.AdminEmail))
        {
            return new RegistrationOutcome(RegistrationError.EmailTaken, null);
        }

        var tenant = new Tenant { Name = req.CompanyName.Trim(), Slug = slug };
        _tenant.SetTenant(tenant.Id);

        var user = new User
        {
            Email = req.AdminEmail.Trim(),
            FullName = req.AdminFullName.Trim(),
            PasswordHash = _hasher.Hash(req.AdminPassword!),
        };
        var role = new Role { Name = "Admin" };

        var permIds = await _db.Permissions.Select(p => p.Id).ToListAsync();
        var rolePermissions = permIds
            .Select(permissionId => new RolePermission { RoleId = role.Id, PermissionId = permissionId })
            .ToList();
        var userRole = new UserRole { UserId = user.Id, RoleId = role.Id };

        // Plan là global (không lọc theo tenant) — gán gói mặc định cho tenant mới tạo.
        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Code == PlanCatalog.DefaultPlanCode);
        var subscription = plan is null
            ? null
            : new Subscription
            {
                PlanId = plan.Id,
                Status = SubscriptionStatus.Active,
                StartedAt = DateTimeOffset.UtcNow,
                ExpiresAt = null,
            };

        _db.Tenants.Add(tenant);
        _db.Users.Add(user);
        _db.Roles.Add(role);
        _db.RolePermissions.AddRange(rolePermissions);
        _db.UserRoles.Add(userRole);
        if (subscription is not null)
        {
            _db.Subscriptions.Add(subscription);
        }

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (IsRegistrationIdentityConflict(ex))
        {
            return new RegistrationOutcome(RegistrationError.Conflict, null);
        }

        return new RegistrationOutcome(RegistrationError.None,
            new RegistrationResponse(tenant.Id, tenant.Slug, user.Id));
    }

    private static bool IsRegistrationIdentityConflict(DbUpdateException exception)
    {
        for (var current = exception.InnerException; current is not null; current = current.InnerException)
        {
            var isConflict = current switch
            {
                PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres =>
                    IsRegistrationIdentity(postgres.ConstraintName),
                SqlServerException { Number: 2601 or 2627 } sqlServer =>
                    ContainsRegistrationIdentity(sqlServer.Message),
                SqliteException { SqliteExtendedErrorCode: 2067 } sqlite =>
                    sqlite.Message.Contains("Users.NormalizedEmail", StringComparison.OrdinalIgnoreCase)
                    || sqlite.Message.Contains("Tenants.Slug", StringComparison.OrdinalIgnoreCase),
                _ => false,
            };

            if (isConflict)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsRegistrationIdentity(string? constraintName)
        => string.Equals(constraintName, UserEmailIndex, StringComparison.Ordinal)
           || string.Equals(constraintName, TenantSlugIndex, StringComparison.Ordinal);

    private static bool ContainsRegistrationIdentity(string message)
        => message.Contains(UserEmailIndex, StringComparison.OrdinalIgnoreCase)
           || message.Contains(TenantSlugIndex, StringComparison.OrdinalIgnoreCase);
}
