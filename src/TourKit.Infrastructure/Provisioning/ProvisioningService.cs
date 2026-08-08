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
        ArgumentNullException.ThrowIfNull(req);
        if ((req.AdminPassword?.Length ?? 0) < 8)
        {
            return new RegistrationOutcome(RegistrationError.Invalid, null);
        }

        return await CapPhatAsync(
            req.CompanyName, req.Slug, req.AdminEmail, req.AdminFullName,
            _hasher.Hash(req.AdminPassword!), lienKetNgoai: null);
    }

    public async Task<RegistrationOutcome> RegisterExternalAsync(RegisterExternalTenantRequest req)
    {
        ArgumentNullException.ThrowIfNull(req);
        if (string.IsNullOrWhiteSpace(req.Provider) || string.IsNullOrWhiteSpace(req.ProviderSubject))
        {
            return new RegistrationOutcome(RegistrationError.Invalid, null);
        }

        // Tài khoản tạo qua nhà cung cấp ngoài vẫn PHẢI có mật khẩu băm, vì cột đó là bắt buộc và vì
        // để trống nghĩa là mở đường cho một tài khoản đăng nhập được bằng mật khẩu rỗng. Sinh 32
        // byte ngẫu nhiên, băm, rồi bỏ — không gán vào entity, không trả ra, không ghi log. Người
        // dùng muốn có mật khẩu thì đi qua chức năng quên mật khẩu như mọi người.
        //
        // Tuyệt đối không dùng một chuỗi cố định kiểu "Google@123": chỉ cần một người đọc mã nguồn
        // là mọi tài khoản tạo bằng Google trên mọi bản cài đặt đều mở được.
        var matKhauBam = _hasher.Hash(
            Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32)));

        return await CapPhatAsync(
            req.CompanyName, req.Slug, req.AdminEmail, req.AdminFullName, matKhauBam,
            lienKetNgoai: (req.Provider, req.ProviderSubject));
    }

    /// <summary>
    /// Lõi dùng chung cho cả hai luồng đăng ký. Gộp lại vì trước đó chỉ khác nhau đúng hai chỗ —
    /// mật khẩu ở đâu ra, và có tạo liên kết nhà cung cấp ngoài hay không. Tách thành hai bản chép
    /// thì lần sau ai sửa quy tắc cấp quyền hoặc gói mặc định sẽ chỉ sửa một bên.
    /// </summary>
    private async Task<RegistrationOutcome> CapPhatAsync(
        string companyName, string slugThô, string email, string fullName,
        string passwordHash, (string Provider, string Subject)? lienKetNgoai)
    {
        var slug = (slugThô ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(companyName)
            || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(fullName))
        {
            return new RegistrationOutcome(RegistrationError.Invalid, null);
        }

        if (await _db.Tenants.AnyAsync(t => t.Slug == slug && !t.IsDeleted))
        {
            return new RegistrationOutcome(RegistrationError.SlugTaken, null);
        }

        if (await _identity.EmailExistsAsync(email))
        {
            return new RegistrationOutcome(RegistrationError.EmailTaken, null);
        }

        var tenant = new Tenant { Name = companyName.Trim(), Slug = slug };
        _tenant.SetTenant(tenant.Id);

        var user = new User
        {
            Email = email.Trim(),
            FullName = fullName.Trim(),
            PasswordHash = passwordHash,
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

        // Liên kết nhà cung cấp nằm TRONG cùng một SaveChanges với tenant/user. Lưu tách ra thì một
        // lần lưu hỏng giữa chừng để lại công ty đã tạo mà người dùng không đăng nhập lại được bằng
        // Google — và cũng không đăng nhập được bằng mật khẩu vì mật khẩu là chuỗi ngẫu nhiên đã vứt.
        if (lienKetNgoai is { } lk)
        {
            _db.UserExternalLogins.Add(new UserExternalLogin
            {
                TenantId = tenant.Id,
                UserId = user.Id,
                Provider = lk.Provider,
                ProviderSubject = lk.Subject,
                ProviderEmail = email.Trim(),
            });
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
