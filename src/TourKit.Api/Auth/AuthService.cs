using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TourKit.Api.Tenancy;
using TourKit.Shared.Entities;
using TourKit.Infrastructure.Persistence;

namespace TourKit.Api.Auth;

public sealed class AuthService : IAuthService
{
    private readonly AppDbContext _db;
    private readonly AmbientTenantContext _tenant;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly JwtOptions _opt;

    public AuthService(AppDbContext db, AmbientTenantContext tenant, IPasswordHasher hasher,
        IJwtTokenService jwt, Microsoft.Extensions.Options.IOptions<JwtOptions> opt)
    {
        _db = db;
        _tenant = tenant;
        _hasher = hasher;
        _jwt = jwt;
        _opt = opt.Value;
    }

    public async Task<AuthResponse?> LoginAsync(LoginRequest req, CancellationToken ct)
    {
        // Tenant KHÔNG phải ITenantEntity → không bị query filter; tra thẳng theo slug.
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Slug == req.TenantSlug && !t.IsDeleted, ct);
        if (tenant is null)
        {
            return null;
        }

        // Chưa có tenant context → phải IgnoreQueryFilters, lọc tay theo tenant + email.
        var user = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.TenantId == tenant.Id && u.Email == req.Email && !u.IsDeleted, ct);
        if (user is null || !user.IsActive || !_hasher.Verify(user.PasswordHash, req.Password))
        {
            return null;
        }

        // Từ đây ĐÃ biết tenant → set ambient để ghi RefreshToken đúng tenant qua interceptor.
        _tenant.SetTenant(tenant.Id);
        return await IssueAsync(user, ct);
    }

    public async Task<AuthResponse?> RefreshAsync(string refreshToken, CancellationToken ct)
    {
        var hash = HashToken(refreshToken);
        var stored = await _db.RefreshTokens.IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.TokenHash == hash, ct);
        if (stored is null || !stored.IsActive)
        {
            return null;
        }

        var user = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Id == stored.UserId && !u.IsDeleted, ct);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        // Chặn refresh cho tenant đã bị soft-delete (đồng bộ với LoginAsync; quan trọng khi 0b-3/0b-4 có deactivate tenant).
        var tenantActive = await _db.Tenants.AnyAsync(t => t.Id == user.TenantId && !t.IsDeleted, ct);
        if (!tenantActive)
        {
            return null;
        }

        _tenant.SetTenant(user.TenantId);
        stored.RevokedAt = DateTimeOffset.UtcNow;   // rotate: thu hồi token cũ
        return await IssueAsync(user, ct);
    }

    private async Task<AuthResponse> IssueAsync(User user, CancellationToken ct)
    {
        var permissions = await LoadPermissionsAsync(user.Id, ct);
        var access = _jwt.CreateAccessToken(user, permissions);
        var refresh = _jwt.CreateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = HashToken(refresh),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(_opt.RefreshTokenDays),
        });
        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new AuthResponse(access, refresh, _jwt.AccessTokenExpiry());
    }

    private async Task<List<string>> LoadPermissionsAsync(Guid userId, CancellationToken ct)
    {
        // UserRole → RolePermission → Permission.Code (RBAC là ITenantEntity nên đã lọc theo tenant hiện tại).
        return await _db.UserRoles.Where(ur => ur.UserId == userId)
            .Join(_db.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (ur, rp) => rp.PermissionId)
            .Join(_db.Permissions, pid => pid, p => p.Id, (pid, p) => p.Code)
            .Distinct()
            .ToListAsync(ct);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
