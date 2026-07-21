using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using TourKit.Application.Auth;
using TourKit.Infrastructure.Persistence;
using TourKit.Infrastructure.Tenancy;

namespace TourKit.Infrastructure.Auth;

/// <summary>
/// Xác thực cho UI Razor Pages (cookie). Dựng ClaimsPrincipal mang claim sub/tenant_id/email/perm
/// KHỚP JWT (JwtTokenService) nên tenancy + RBAC + interceptor dùng chung, không phụ thuộc scheme.
/// </summary>
public sealed class CookieAuthService : ICookieAuthService
{
    private readonly AppDbContext _db;
    private readonly AmbientTenantContext _tenant;
    private readonly IPasswordHasher _hasher;

    public CookieAuthService(AppDbContext db, AmbientTenantContext tenant, IPasswordHasher hasher)
    {
        _db = db;
        _tenant = tenant;
        _hasher = hasher;
    }

    public async Task<ClaimsPrincipal?> AuthenticateAsync(string tenantSlug, string email, string password)
    {
        // Tenant KHÔNG phải ITenantEntity → không bị query filter; tra thẳng theo slug.
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Slug == tenantSlug && !t.IsDeleted);
        if (tenant is null)
        {
            return null;
        }

        // Chưa có tenant context → IgnoreQueryFilters, lọc tay theo tenant + email (giống AuthService).
        var user = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.TenantId == tenant.Id && u.Email == email && !u.IsDeleted);
        if (user is null || !user.IsActive || !_hasher.Verify(user.PasswordHash, password))
        {
            return null;
        }

        // Đã biết tenant → set ambient để LoadPermissions lọc RBAC đúng tenant.
        _tenant.SetTenant(tenant.Id);

        var permissions = await _db.UserRoles.Where(ur => ur.UserId == user.Id)
            .Join(_db.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (ur, rp) => rp.PermissionId)
            .Join(_db.Permissions, pid => pid, p => p.Id, (pid, p) => p.Code)
            .Distinct()
            .ToListAsync();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("tenant_id", user.TenantId.ToString()),
            new("email", user.Email),
            // Tên hiển thị đi kèm cookie: entity user đã nạp sẵn ở trên nên claim này KHÔNG tốn thêm
            // truy vấn nào, mà mọi màn cần chào tên/hiện tên người dùng thì khỏi phải tra bảng Users.
            new("name", user.FullName),
        };
        claims.AddRange(permissions.Select(code => new Claim("perm", code)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, "email", "perm");
        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return new ClaimsPrincipal(identity);
    }
}
