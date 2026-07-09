using Microsoft.EntityFrameworkCore;
using TourKit.Api.Auth;
using TourKit.Api.Billing;
using TourKit.Api.Tenancy;
using TourKit.Shared.Entities;
using TourKit.Infrastructure.Persistence;

using TourKit.Shared.Enums;

namespace TourKit.Api.Provisioning;

/// <summary>
/// Tạo tenant mới + user admin + role "Admin" (đủ quyền) trong một lần đăng ký.
/// Lưu ý atomicity: các SaveChanges tuần tự, chưa bọc transaction (InMemory test không hỗ trợ).
/// Follow-up khi lên prod (SqlServer/PostgreSQL): bọc BeginTransaction để đảm bảo toàn vẹn.
/// </summary>
public sealed class ProvisioningService : IProvisioningService
{
    private readonly AppDbContext _db;
    private readonly AmbientTenantContext _tenant;
    private readonly IPasswordHasher _hasher;

    public ProvisioningService(AppDbContext db, AmbientTenantContext tenant, IPasswordHasher hasher)
    {
        _db = db;
        _tenant = tenant;
        _hasher = hasher;
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

        var tenant = new Tenant { Name = req.CompanyName.Trim(), Slug = slug };
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync();

        _tenant.SetTenant(tenant.Id);   // để interceptor gán TenantId cho user/role

        var user = new User
        {
            Email = req.AdminEmail.Trim(),
            FullName = req.AdminFullName.Trim(),
            PasswordHash = _hasher.Hash(req.AdminPassword!),
        };
        _db.Users.Add(user);

        var role = new Role { Name = "Admin" };
        _db.Roles.Add(role);
        await _db.SaveChangesAsync();

        var permIds = await _db.Permissions.Select(p => p.Id).ToListAsync();
        foreach (var pid in permIds)
        {
            _db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = pid });
        }

        _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });

        // Plan là global (không lọc theo tenant) — gán gói mặc định cho tenant mới tạo.
        var plan = await _db.Plans.FirstOrDefaultAsync(p => p.Code == PlanCatalog.DefaultPlanCode);
        if (plan is not null)
        {
            _db.Subscriptions.Add(new Subscription
            {
                PlanId = plan.Id,
                Status = SubscriptionStatus.Active,
                StartedAt = DateTimeOffset.UtcNow,
                ExpiresAt = null,
            });
        }

        await _db.SaveChangesAsync();

        return new RegistrationOutcome(RegistrationError.None,
            new RegistrationResponse(tenant.Id, tenant.Slug, user.Id));
    }
}
