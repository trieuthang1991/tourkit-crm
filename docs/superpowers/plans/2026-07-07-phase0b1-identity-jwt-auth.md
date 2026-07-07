# Phase 0b-1 — Identity + JWT Auth — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **BẮT BUỘC ĐỌC TRƯỚC:** `docs/conventions/backend-conventions.md` (3 luật vàng, tenancy §4, EF §5, API §6). Enforcement `.editorconfig` + `Directory.Build.props` sẽ ép: net9, nullable, warnings-as-errors, braces bắt buộc, file-scoped namespace, sealed record, không using thừa.

**Goal:** Người dùng của một tenant đăng nhập bằng `tenantSlug + email + password`, nhận JWT chứa `tenant_id`; các endpoint nghiệp vụ yêu cầu JWT hợp lệ và tự cô lập dữ liệu theo tenant lấy từ claim (thay cơ chế header tạm của Phase 0a).

**Architecture:** Thêm `User` + `RefreshToken` (ITenantEntity) vào Infrastructure. Thay `HttpTenantContext` (đọc header) bằng `AmbientTenantContext` — một `ITenantContext` scoped **có thể set tenant** (từ claim JWT qua middleware, hoặc set tay khi login/seed). Auth logic (`PasswordHasher`, `JwtTokenService`, `AuthService`) đặt ở `TourKit.Api` (composition root; sẽ tách module `Identity` khi lớn hơn). Login resolve tenant theo slug → tìm user bằng `IgnoreQueryFilters` (chưa có tenant context) → verify hash → set ambient tenant → phát JWT + refresh token.

**Tech Stack:** .NET 9, EF Core 9 (SQLite dev), ASP.NET Core JWT Bearer, `PasswordHasher<T>` (Microsoft.Extensions.Identity.Core), `System.IdentityModel.Tokens.Jwt`, xUnit + Mvc.Testing.

**Phạm vi Phase 0b** được tách thành các plan con (mỗi cái chạy & test độc lập): **0b-1 Identity+JWT (plan này)** · 0b-2 RBAC + data-scope · 0b-3 Đăng ký & provisioning tenant · 0b-4 Subscription/Plan · 0b-5 React shell + login. Roles/permissions CHƯA có ở 0b-1 (JWT chưa mang role) — thêm ở 0b-2.

---

## File Structure

```
src/TourKit.Shared/
  Tenancy/ITenantContext.cs            # (có sẵn) — không đổi
src/TourKit.Infrastructure/
  Entities/User.cs                     # NEW — ITenantEntity: Email, PasswordHash, FullName, Status
  Entities/RefreshToken.cs             # NEW — ITenantEntity: UserId, TokenHash, ExpiresAt, RevokedAt
  Persistence/AppDbContext.cs          # MODIFY — thêm DbSet<User>, DbSet<RefreshToken>
  Persistence/Configurations/UserConfiguration.cs         # NEW
  Persistence/Configurations/RefreshTokenConfiguration.cs # NEW
  Migrations/*_AddIdentity.cs          # NEW (sinh bằng dotnet ef)
src/TourKit.Api/
  Tenancy/AmbientTenantContext.cs      # NEW — thay HttpTenantContext
  Tenancy/TenantResolutionMiddleware.cs# NEW — set tenant từ claim JWT
  Auth/JwtOptions.cs                   # NEW — bind config "Jwt"
  Auth/IPasswordHasher.cs + PasswordHasher.cs   # NEW
  Auth/IJwtTokenService.cs + JwtTokenService.cs # NEW
  Auth/IAuthService.cs + AuthService.cs         # NEW
  Auth/AuthContracts.cs                # NEW — LoginRequest/RefreshRequest/AuthResponse
  Auth/AuthEndpoints.cs                # NEW — /api/v1/auth/login + /refresh
  Program.cs                           # MODIFY — DI auth + JWT + middleware + protect customers
  Tenancy/HttpTenantContext.cs         # DELETE (thay bằng AmbientTenantContext)
  appsettings.json                     # MODIFY — thêm mục "Jwt"
tests/TourKit.Tests/
  Support/AuthTestFactory.cs           # NEW — WebAppFactory InMemory + seed tenant/user
  Auth/PasswordHasherTests.cs          # NEW
  Auth/JwtTokenServiceTests.cs         # NEW
  Auth/AuthEndpointTests.cs            # NEW — login + refresh + isolation qua JWT
```

**Nguyên tắc quyết định:** Auth ở `TourKit.Api` (chưa tách module `Identity` — YAGNI ở 0b-1). `AmbientTenantContext` là điểm mấu chốt: một `ITenantContext` scoped, mutable, nguồn tenant có thể là claim JWT hoặc set-tay (login/seed/provisioning sau này) — thống nhất một cơ chế thay vì mỗi nơi một kiểu.

---

### Task 1: NuGet packages + cấu hình JWT

**Files:**
- Modify: `src/TourKit.Api/TourKit.Api.csproj`, `src/TourKit.Api/appsettings.json`
- Create: `src/TourKit.Api/Auth/JwtOptions.cs`

- [ ] **Step 1: Cài package**

```bash
dotnet add src/TourKit.Api package Microsoft.AspNetCore.Authentication.JwtBearer -v 9.0.*
dotnet add src/TourKit.Api package Microsoft.Extensions.Identity.Core -v 9.0.*
dotnet add src/TourKit.Api package System.IdentityModel.Tokens.Jwt -v 8.*
```

- [ ] **Step 2: `JwtOptions` (POCO bind config)**

Create `src/TourKit.Api/Auth/JwtOptions.cs`:

```csharp
namespace TourKit.Api.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "tourkit";
    public string Audience { get; set; } = "tourkit";
    public string Secret { get; set; } = string.Empty;       // >= 32 ký tự; nạp từ config/secret
    public int AccessTokenMinutes { get; set; } = 30;
    public int RefreshTokenDays { get; set; } = 14;
}
```

- [ ] **Step 3: Thêm mục `Jwt` vào `appsettings.json`**

Thêm khối `"Jwt"` (dev secret — production nạp qua env/secret store) vào `src/TourKit.Api/appsettings.json`, ngang cấp với `"Database"`:

```json
  "Jwt": {
    "Issuer": "tourkit",
    "Audience": "tourkit",
    "Secret": "dev-only-super-secret-key-change-me-32chars!",
    "AccessTokenMinutes": 30,
    "RefreshTokenDays": 14
  },
```

- [ ] **Step 4: Build**

Run: `dotnet build`
Expected: Build succeeded, 0 Warning, 0 Error.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "chore(api): thêm package JWT + Identity hashing + JwtOptions"
```

---

### Task 2: Entities `User` + `RefreshToken` + config + migration

**Files:**
- Create: `src/TourKit.Infrastructure/Entities/User.cs`, `Entities/RefreshToken.cs`
- Create: `src/TourKit.Infrastructure/Persistence/Configurations/UserConfiguration.cs`, `RefreshTokenConfiguration.cs`
- Modify: `src/TourKit.Infrastructure/Persistence/AppDbContext.cs`

- [ ] **Step 1: `User`**

Create `src/TourKit.Infrastructure/Entities/User.cs`:

```csharp
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Entities;

public class User : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset? LastLoginAt { get; set; }
}
```

- [ ] **Step 2: `RefreshToken`**

Create `src/TourKit.Infrastructure/Entities/RefreshToken.cs`:

```csharp
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Entities;

public class RefreshToken : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;   // lưu HASH, không lưu token gốc
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTimeOffset.UtcNow;
}
```

- [ ] **Step 3: Config `User`**

Create `src/TourKit.Infrastructure/Persistence/Configurations/UserConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Infrastructure.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(x => x.Email).IsRequired().HasMaxLength(256);
        builder.Property(x => x.PasswordHash).IsRequired().HasMaxLength(512);
        builder.Property(x => x.FullName).IsRequired().HasMaxLength(200);

        // Email duy nhất TRONG phạm vi tenant (login theo tenantSlug + email).
        builder.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
    }
}
```

- [ ] **Step 4: Config `RefreshToken`**

Create `src/TourKit.Infrastructure/Persistence/Configurations/RefreshTokenConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Infrastructure.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.Property(x => x.TokenHash).IsRequired().HasMaxLength(128);
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.UserId });
    }
}
```

- [ ] **Step 5: Thêm DbSet vào `AppDbContext`**

Trong `src/TourKit.Infrastructure/Persistence/AppDbContext.cs`, thêm 2 DbSet ngay dưới `Customers`:

```csharp
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
```

(`using TourKit.Infrastructure.Entities;` đã có. `ApplyConfigurationsFromAssembly` đã tự nạp 2 config mới. Query filter tenant + soft-delete tự áp cho `User`/`RefreshToken` vì là `ITenantEntity`.)

- [ ] **Step 6: Build + tạo migration**

```bash
dotnet build
dotnet ef migrations add AddIdentity --project src/TourKit.Infrastructure --startup-project src/TourKit.Api
dotnet ef database update --project src/TourKit.Infrastructure --startup-project src/TourKit.Api
```
Expected: migration tạo bảng `Users`, `RefreshTokens`; `database update` in `Done.`

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat(infra): User + RefreshToken entities + config + migration AddIdentity"
```

---

### Task 3: `AmbientTenantContext` thay `HttpTenantContext`

**Files:**
- Create: `src/TourKit.Api/Tenancy/AmbientTenantContext.cs`
- Delete: `src/TourKit.Api/Tenancy/HttpTenantContext.cs`
- Test: `tests/TourKit.Tests/Tenancy/AmbientTenantContextTests.cs`

- [ ] **Step 1: Viết test (FAIL trước — class chưa có)**

Create `tests/TourKit.Tests/Tenancy/AmbientTenantContextTests.cs`:

```csharp
using TourKit.Api.Tenancy;

namespace TourKit.Tests.Tenancy;

public class AmbientTenantContextTests
{
    [Fact]
    public void Starts_with_no_tenant_then_can_be_set()
    {
        var ctx = new AmbientTenantContext();
        Assert.False(ctx.HasTenant);

        var id = Guid.NewGuid();
        ctx.SetTenant(id);

        Assert.True(ctx.HasTenant);
        Assert.Equal(id, ctx.TenantId);
    }
}
```

- [ ] **Step 2: Chạy — kỳ vọng FAIL biên dịch** (`AmbientTenantContext` chưa tồn tại)

Run: `dotnet build tests/TourKit.Tests`
Expected: FAIL — type or namespace 'AmbientTenantContext' not found.

- [ ] **Step 3: Viết `AmbientTenantContext`**

Create `src/TourKit.Api/Tenancy/AmbientTenantContext.cs`:

```csharp
using TourKit.Shared.Tenancy;

namespace TourKit.Api.Tenancy;

/// <summary>
/// Tenant của request hiện tại — nguồn có thể là claim JWT (qua TenantResolutionMiddleware)
/// hoặc set tường minh khi login/seed/provisioning (chưa có claim). Scoped: mỗi request 1 instance.
/// </summary>
public sealed class AmbientTenantContext : ITenantContext
{
    public Guid TenantId { get; private set; }
    public bool HasTenant => TenantId != Guid.Empty;

    public void SetTenant(Guid tenantId) => TenantId = tenantId;
}
```

- [ ] **Step 4: Xóa `HttpTenantContext`**

```bash
git rm src/TourKit.Api/Tenancy/HttpTenantContext.cs
```

(Program.cs sẽ được sửa để dùng `AmbientTenantContext` ở Task 6 — build có thể tạm đỏ ở Program cho tới Task 6; chạy test class này bằng `dotnet test --filter AmbientTenantContext` sau khi Task 6 wire xong. Nếu muốn xanh ngay, làm Task 6 Step wiring trước rồi quay lại.)

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(api): AmbientTenantContext (mutable) thay HttpTenantContext"
```

---

### Task 4: `PasswordHasher` (TDD)

**Files:**
- Create: `src/TourKit.Api/Auth/IPasswordHasher.cs`, `Auth/PasswordHasher.cs`
- Test: `tests/TourKit.Tests/Auth/PasswordHasherTests.cs`

- [ ] **Step 1: Viết test (FAIL trước)**

Create `tests/TourKit.Tests/Auth/PasswordHasherTests.cs`:

```csharp
using TourKit.Api.Auth;

namespace TourKit.Tests.Auth;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_then_verify_true()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.Hash("S3cret!");

        Assert.NotEqual("S3cret!", hash);      // không lưu plain
        Assert.True(hasher.Verify(hash, "S3cret!"));
    }

    [Fact]
    public void Verify_wrong_password_false()
    {
        var hasher = new PasswordHasher();
        var hash = hasher.Hash("S3cret!");

        Assert.False(hasher.Verify(hash, "wrong"));
    }
}
```

- [ ] **Step 2: Build test — kỳ vọng FAIL** (`PasswordHasher` chưa có)

Run: `dotnet build tests/TourKit.Tests`
Expected: FAIL — 'PasswordHasher' not found.

- [ ] **Step 3: Viết interface + impl**

Create `src/TourKit.Api/Auth/IPasswordHasher.cs`:

```csharp
namespace TourKit.Api.Auth;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string hash, string password);
}
```

Create `src/TourKit.Api/Auth/PasswordHasher.cs`:

```csharp
using Microsoft.AspNetCore.Identity;
using TourKit.Infrastructure.Entities;

namespace TourKit.Api.Auth;

public sealed class PasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _inner = new();

    public string Hash(string password) => _inner.HashPassword(new User(), password);

    public bool Verify(string hash, string password)
    {
        var result = _inner.VerifyHashedPassword(new User(), hash, password);
        return result is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
```

- [ ] **Step 4: Chạy test — kỳ vọng PASS**

Run: `dotnet test --filter PasswordHasherTests`
Expected: Passed! 2 tests.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(api): PasswordHasher (Identity PBKDF2) + test"
```

---

### Task 5: `JwtTokenService` (TDD)

**Files:**
- Create: `src/TourKit.Api/Auth/IJwtTokenService.cs`, `Auth/JwtTokenService.cs`
- Test: `tests/TourKit.Tests/Auth/JwtTokenServiceTests.cs`

- [ ] **Step 1: Viết test (FAIL trước)**

Create `tests/TourKit.Tests/Auth/JwtTokenServiceTests.cs`:

```csharp
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using TourKit.Api.Auth;
using TourKit.Infrastructure.Entities;

namespace TourKit.Tests.Auth;

public class JwtTokenServiceTests
{
    private static JwtTokenService Create()
    {
        var opt = Options.Create(new JwtOptions
        {
            Issuer = "tourkit",
            Audience = "tourkit",
            Secret = "test-secret-key-at-least-32-characters-long!",
            AccessTokenMinutes = 30,
        });
        return new JwtTokenService(opt);
    }

    [Fact]
    public void Access_token_carries_tenant_and_user_claims()
    {
        var svc = Create();
        var user = new User { TenantId = Guid.NewGuid(), Email = "a@b.com", FullName = "A" };

        var token = svc.CreateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(user.Id.ToString(), jwt.Subject);
        Assert.Equal(user.TenantId.ToString(), jwt.Claims.First(c => c.Type == "tenant_id").Value);
        Assert.Equal("a@b.com", jwt.Claims.First(c => c.Type == "email").Value);
    }

    [Fact]
    public void Refresh_token_is_random_and_long()
    {
        var svc = Create();
        var a = svc.CreateRefreshToken();
        var b = svc.CreateRefreshToken();

        Assert.NotEqual(a, b);
        Assert.True(a.Length >= 32);
    }
}
```

- [ ] **Step 2: Build test — kỳ vọng FAIL** (`JwtTokenService` chưa có)

Run: `dotnet build tests/TourKit.Tests`
Expected: FAIL — 'JwtTokenService' not found.

- [ ] **Step 3: Viết interface + impl**

Create `src/TourKit.Api/Auth/IJwtTokenService.cs`:

```csharp
using TourKit.Infrastructure.Entities;

namespace TourKit.Api.Auth;

public interface IJwtTokenService
{
    string CreateAccessToken(User user);
    string CreateRefreshToken();
    DateTimeOffset AccessTokenExpiry();
}
```

Create `src/TourKit.Api/Auth/JwtTokenService.cs`:

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TourKit.Infrastructure.Entities;

namespace TourKit.Api.Auth;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _opt;

    public JwtTokenService(IOptions<JwtOptions> opt) => _opt = opt.Value;

    public DateTimeOffset AccessTokenExpiry() =>
        DateTimeOffset.UtcNow.AddMinutes(_opt.AccessTokenMinutes);

    public string CreateAccessToken(User user)
    {
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opt.Secret)),
            SecurityAlgorithms.HmacSha256);

        Claim[] claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("tenant_id", user.TenantId.ToString()),
            new("email", user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        ];

        var token = new JwtSecurityToken(
            issuer: _opt.Issuer,
            audience: _opt.Audience,
            claims: claims,
            expires: AccessTokenExpiry().UtcDateTime,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes);
    }
}
```

- [ ] **Step 4: Chạy test — kỳ vọng PASS**

Run: `dotnet test --filter JwtTokenServiceTests`
Expected: Passed! 2 tests.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(api): JwtTokenService (HS256, claim tenant_id/sub/email) + test"
```

---

### Task 6: `AuthService` + wire DI/JWT/middleware + endpoints + protect customers

**Files:**
- Create: `src/TourKit.Api/Auth/IAuthService.cs`, `Auth/AuthService.cs`, `Auth/AuthContracts.cs`, `Auth/AuthEndpoints.cs`
- Create: `src/TourKit.Api/Tenancy/TenantResolutionMiddleware.cs`
- Modify: `src/TourKit.Api/Program.cs`, `src/TourKit.Api/Customers/CustomerEndpoints.cs`

- [ ] **Step 1: DTO auth**

Create `src/TourKit.Api/Auth/AuthContracts.cs`:

```csharp
namespace TourKit.Api.Auth;

public sealed record LoginRequest(string TenantSlug, string Email, string Password);
public sealed record RefreshRequest(string RefreshToken);
public sealed record AuthResponse(string AccessToken, string RefreshToken, DateTimeOffset AccessTokenExpiresAt);
```

- [ ] **Step 2: `IAuthService`**

Create `src/TourKit.Api/Auth/IAuthService.cs`:

```csharp
namespace TourKit.Api.Auth;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest req, CancellationToken ct);
    Task<AuthResponse?> RefreshAsync(string refreshToken, CancellationToken ct);
}
```

- [ ] **Step 3: `AuthService`**

Create `src/TourKit.Api/Auth/AuthService.cs`:

```csharp
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using TourKit.Api.Tenancy;
using TourKit.Infrastructure.Entities;
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

        _tenant.SetTenant(user.TenantId);
        stored.RevokedAt = DateTimeOffset.UtcNow;   // rotate: thu hồi token cũ
        return await IssueAsync(user, ct);
    }

    private async Task<AuthResponse> IssueAsync(User user, CancellationToken ct)
    {
        var access = _jwt.CreateAccessToken(user);
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

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
```

- [ ] **Step 4: Middleware set tenant từ claim**

Create `src/TourKit.Api/Tenancy/TenantResolutionMiddleware.cs`:

```csharp
namespace TourKit.Api.Tenancy;

public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, AmbientTenantContext tenant)
    {
        var claim = context.User.FindFirst("tenant_id")?.Value;
        if (Guid.TryParse(claim, out var tenantId))
        {
            tenant.SetTenant(tenantId);
        }

        await _next(context);
    }
}
```

- [ ] **Step 5: Endpoints auth**

Create `src/TourKit.Api/Auth/AuthEndpoints.cs`:

```csharp
namespace TourKit.Api.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth");

        group.MapPost("/login", async (LoginRequest body, IAuthService auth, CancellationToken ct) =>
        {
            var result = await auth.LoginAsync(body, ct);
            return result is null
                ? Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Đăng nhập thất bại.")
                : Results.Ok(result);
        }).AllowAnonymous();

        group.MapPost("/refresh", async (RefreshRequest body, IAuthService auth, CancellationToken ct) =>
        {
            var result = await auth.RefreshAsync(body.RefreshToken, ct);
            return result is null
                ? Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Refresh token không hợp lệ.")
                : Results.Ok(result);
        }).AllowAnonymous();

        return app;
    }
}
```

- [ ] **Step 6: Sửa `Program.cs`** — thay đăng ký tenant, thêm JWT + auth services + middleware, bảo vệ customers

Thay toàn bộ `src/TourKit.Api/Program.cs`:

```csharp
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TourKit.Api.Auth;
using TourKit.Api.Customers;
using TourKit.Api.Tenancy;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Tenancy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();

// --- Tenancy: 1 instance scoped, vừa là ITenantContext (đọc) vừa set được (login/middleware) ---
builder.Services.AddScoped<AmbientTenantContext>();
builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<AmbientTenantContext>());

// --- DB provider theo cấu hình ---
var provider = builder.Configuration["Database:Provider"] ?? "Sqlite";
var connectionString = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    if (string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase))
    {
        opt.UseSqlServer(connectionString);
    }
    else
    {
        opt.UseSqlite(connectionString);
    }
});

// --- Auth services ---
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
            ValidateLifetime = true,
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseAuthentication();
app.UseMiddleware<TenantResolutionMiddleware>();   // sau Authentication để đọc được claim
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapCustomerEndpoints();

app.Run();

// Cho phép WebApplicationFactory trong test truy cập Program.
public partial class Program { }
```

- [ ] **Step 7: Bảo vệ customers bằng JWT**

Trong `src/TourKit.Api/Customers/CustomerEndpoints.cs`, thêm `.RequireAuthorization()` cho group. Sửa dòng tạo group:

```csharp
        var group = app.MapGroup("/api/v1/customers").RequireAuthorization();
```

- [ ] **Step 8: Build**

Run: `dotnet build`
Expected: Build succeeded, 0 Warning, 0 Error.

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "feat(api): AuthService (login/refresh) + JWT auth + tenant middleware + bảo vệ /customers"
```

---

### Task 7: Test tích hợp — login, refresh, cô lập qua JWT

**Files:**
- Create: `tests/TourKit.Tests/Support/AuthTestFactory.cs`
- Create: `tests/TourKit.Tests/Auth/AuthEndpointTests.cs`
- Modify: `tests/TourKit.Tests/Api/CustomerEndpointIsolationTests.cs` (endpoint /customers giờ cần JWT)

- [ ] **Step 1: Factory InMemory + seed tenant/user**

Create `tests/TourKit.Tests/Support/AuthTestFactory.cs`:

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TourKit.Api.Auth;
using TourKit.Api.Tenancy;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;

namespace TourKit.Tests.Support;

/// <summary>WebApplicationFactory dùng InMemory + tiện ích seed tenant/user để test auth.</summary>
public sealed class AuthTestFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "AuthTests-" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                (d.ServiceType.IsGenericType &&
                 d.ServiceType.GetGenericTypeDefinition().Name == "IDbContextOptionsConfiguration`1" &&
                 d.ServiceType.GenericTypeArguments[0] == typeof(AppDbContext))).ToList();
            foreach (var d in toRemove)
            {
                services.Remove(d);
            }

            services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase(_dbName));
        });
    }

    /// <summary>Tạo tenant + 1 user (mật khẩu đã hash). Trả về slug + email + password để login.</summary>
    public async Task<(string slug, string email, string password)> SeedTenantUserAsync(string slug)
    {
        using var scope = Services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<AppDbContext>();
        var hasher = sp.GetRequiredService<IPasswordHasher>();
        var tenantCtx = sp.GetRequiredService<AmbientTenantContext>();

        var tenant = new Tenant { Name = slug, Slug = slug };
        db.Tenants.Add(tenant);
        await db.SaveChangesAsync();

        tenantCtx.SetTenant(tenant.Id);   // để interceptor gán TenantId cho User
        var email = $"admin@{slug}.com";
        const string password = "P@ssw0rd!";
        db.Users.Add(new User
        {
            Email = email,
            FullName = "Admin",
            PasswordHash = hasher.Hash(password),
        });
        await db.SaveChangesAsync();

        return (slug, email, password);
    }
}
```

- [ ] **Step 2: Test login + refresh + cô lập qua JWT (FAIL trước khi chạy toàn bộ vì chưa seed đúng — nhưng viết đủ)**

Create `tests/TourKit.Tests/Auth/AuthEndpointTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Tests.Support;

namespace TourKit.Tests.Auth;

public class AuthEndpointTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public AuthEndpointTests(AuthTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_valid_returns_token_then_can_call_protected_endpoint()
    {
        var (slug, email, password) = await _factory.SeedTenantUserAsync("acme");
        var client = _factory.CreateClient();

        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(slug, email, password));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);

        // Không token → 401
        var anon = await client.GetAsync("/api/v1/customers");
        Assert.Equal(HttpStatusCode.Unauthorized, anon.StatusCode);

        // Có token → 200
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        var ok = await client.GetAsync("/api/v1/customers");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
    }

    [Fact]
    public async Task Login_wrong_password_returns_401()
    {
        var (slug, email, _) = await _factory.SeedTenantUserAsync("beta");
        var client = _factory.CreateClient();

        var res = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(slug, email, "wrong-password"));
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Two_tenants_are_isolated_via_jwt()
    {
        var a = await _factory.SeedTenantUserAsync("gamma");
        var b = await _factory.SeedTenantUserAsync("delta");
        var client = _factory.CreateClient();

        // user A tạo khách
        var loginA = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(a.slug, a.email, a.password))).Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginA!.AccessToken);
        await client.PostAsJsonAsync("/api/v1/customers", new { FullName = "Khach cua A", Phone = (string?)null });

        // user B đọc — không thấy khách của A
        var loginB = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(b.slug, b.email, b.password))).Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginB!.AccessToken);
        var listB = await client.GetFromJsonAsync<List<CustomerRow>>("/api/v1/customers");

        Assert.NotNull(listB);
        Assert.Empty(listB!);

        // refresh token của A vẫn cấp token mới
        var refreshed = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(loginA.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
    }

    private sealed record CustomerRow(Guid Id, string FullName, string? Phone);
}
```

- [ ] **Step 3: Cập nhật `CustomerEndpointIsolationTests`** — endpoint giờ cần JWT

Test cũ ở `tests/TourKit.Tests/Api/CustomerEndpointIsolationTests.cs` dùng header `X-Tenant-Id` (không còn tác dụng — endpoint đã `RequireAuthorization`). Thay bằng flow qua `AuthTestFactory` + login lấy JWT. Thay toàn bộ nội dung file:

```csharp
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Tests.Support;

namespace TourKit.Tests.Api;

public class CustomerEndpointIsolationTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public CustomerEndpointIsolationTests(AuthTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Each_tenant_sees_only_its_own_customers_over_http()
    {
        var a = await _factory.SeedTenantUserAsync("acme-iso");
        var b = await _factory.SeedTenantUserAsync("beta-iso");
        var client = _factory.CreateClient();

        var tokenA = (await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(a.slug, a.email, a.password))).Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        (await client.PostAsJsonAsync("/api/v1/customers",
            new { FullName = "A-http", Phone = (string?)null })).EnsureSuccessStatusCode();

        var tokenB = (await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(b.slug, b.email, b.password))).Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var listB = await client.GetFromJsonAsync<List<CustomerDto>>("/api/v1/customers");

        Assert.NotNull(listB);
        Assert.Empty(listB!);
    }

    private sealed record CustomerDto(Guid Id, string FullName, string? Phone);
}
```

- [ ] **Step 4: Chạy toàn bộ test — kỳ vọng PASS**

Run: `dotnet test`
Expected: Passed! Tất cả (tenancy Phase 0a + password + jwt + auth endpoint + isolation qua JWT).

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "test(api): login/refresh + cô lập tenant qua JWT; chuyển endpoint test sang auth thật"
```

---

## Self-Review

**Spec coverage (0b-1):**
- User/RefreshToken entity + migration → Task 2 ✅
- Password hashing → Task 4 ✅
- JWT phát hành + claim tenant_id → Task 5 ✅
- Login theo tenantSlug+email+password, resolve tenant + verify → Task 6 (AuthService) ✅
- Refresh token (rotate) → Task 6 ✅
- Tenant từ JWT claim (thay header Phase 0a) → Task 3 (AmbientTenantContext) + Task 6 (middleware) ✅
- Bảo vệ endpoint + test cô lập qua JWT → Task 6/7 ✅

**Ngoài phạm vi (đúng chủ đích, sang plan sau):** RBAC/role/permission trong JWT (0b-2), đăng ký & provisioning tenant (0b-3), subscription/plan gating (0b-4), React shell + màn login (0b-5). Seed tenant/user hiện chỉ ở test helper — provisioning thật ở 0b-3.

**Placeholder scan:** Không có TBD/TODO; mọi step có code hoặc lệnh cụ thể + output kỳ vọng. ✅

**Type consistency:** `AmbientTenantContext.SetTenant`, `IPasswordHasher.{Hash,Verify}`, `IJwtTokenService.{CreateAccessToken,CreateRefreshToken,AccessTokenExpiry}`, `IAuthService.{LoginAsync,RefreshAsync}`, `AuthResponse(AccessToken,RefreshToken,AccessTokenExpiresAt)`, `LoginRequest(TenantSlug,Email,Password)` dùng nhất quán Task 3→7. ✅

**Rủi ro đã lường:**
- Interceptor ép `TenantId` khi ghi `RefreshToken` lúc login → giải bằng `AmbientTenantContext.SetTenant` sau khi resolve user (Task 6 Step 3).
- `User` là `ITenantEntity` nên bị query filter; login đọc bằng `IgnoreQueryFilters` + lọc tay theo tenant (Task 6 Step 3).
- Endpoint `/customers` sau khi `RequireAuthorization` làm test header cũ hỏng → cập nhật test sang JWT (Task 7 Step 3).
