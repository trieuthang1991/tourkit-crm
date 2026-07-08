# Phase 0b-3 — Đăng ký & Provisioning Tenant — Implementation Plan

> **For agentic workers:** Có thể chạy inline (executing-plans) hoặc subagent-driven. Steps dùng checkbox.
>
> **ĐỌC TRƯỚC:** `backend-conventions.md` (§4 tenancy, §6 API), `database-optimization-analysis.md` §J. Enforcement: net9, warnings-as-errors, braces, sealed record, file-scoped ns.

**Goal:** Một doanh nghiệp mới **tự đăng ký** qua `POST /api/v1/registration` → hệ tạo `Tenant` + user admin + role "Admin" (đủ quyền) + gán, trong một thao tác. Sau đăng ký, admin login được và có toàn quyền. Khép kín vòng Auth (0b-1) + RBAC (0b-2): tenant/user giờ tạo được thật, không chỉ qua test seeder.

**Architecture:** `ProvisioningService` (Api) thực hiện tuần tự: kiểm slug chưa dùng → tạo `Tenant` (global) → `SetTenant` ambient → tạo admin `User` (mật khẩu hash) → tạo role `Admin` gán **tất cả** `Permission` → gán `UserRole`. Endpoint `POST /api/v1/registration` (AllowAnonymous) map kết quả 201 / 409 (slug trùng) / 400 (validate). KHÔNG cần migration (entity đã có từ 0b-1/0b-2).

**Tech Stack:** .NET 9, EF Core 9, ASP.NET Minimal API. Không entity mới → không migration.

**Lưu ý atomicity:** provisioning là chuỗi `SaveChanges` tuần tự (InMemory test không hỗ trợ transaction). Nếu lỗi giữa chừng → tenant dở dang. Chấp nhận ở MVP; follow-up: bọc transaction (SqlServer/PostgreSQL) khi lên prod. Ghi rõ trong code.

**Phạm vi vs sau:** 0b-3 = self-registration + admin provisioning. Defer: mời thêm user vào tenant (invite), CRUD role/permission qua API, email verify, chọn plan lúc đăng ký (0b-4).

---

## File Structure

```
src/TourKit.Api/
  Provisioning/RegistrationContracts.cs   # NEW — RegisterTenantRequest, RegistrationResponse
  Provisioning/IProvisioningService.cs    # NEW
  Provisioning/ProvisioningService.cs     # NEW — tạo tenant+admin+role+perms
  Provisioning/RegistrationEndpoints.cs   # NEW — POST /api/v1/registration (anonymous)
  Program.cs                              # MODIFY — AddScoped provisioning + MapRegistrationEndpoints
tests/TourKit.Tests/
  Provisioning/RegistrationEndpointTests.cs # NEW — register→login→admin quyền; slug trùng 409
```

**Nguyên tắc:** Provisioning ở `Api/Provisioning` (composition root). Dùng lại `IPasswordHasher`, `AmbientTenantContext`, `AppDbContext`, catalog `Permissions`. Đăng ký KHÔNG auto-login (tách bạch: đăng ký xong client gọi `/auth/login`) — đơn giản, dễ test, ít phụ thuộc.

---

### Task 1: Contracts + ProvisioningService (TDD)

**Files:**
- Create: `Provisioning/RegistrationContracts.cs`, `IProvisioningService.cs`, `ProvisioningService.cs`
- Test: `tests/TourKit.Tests/Provisioning/RegistrationEndpointTests.cs` (khởi tạo file, test service qua endpoint ở Task 2 — ở đây test service trực tiếp)

- [ ] **Step 1: Contracts**

```csharp
namespace TourKit.Api.Provisioning;

public sealed record RegisterTenantRequest(
    string CompanyName, string Slug, string AdminEmail, string AdminPassword, string AdminFullName);

public sealed record RegistrationResponse(Guid TenantId, string Slug, Guid AdminUserId);
```

- [ ] **Step 2: Interface + kết quả**

```csharp
namespace TourKit.Api.Provisioning;

public enum RegistrationError { None, SlugTaken, Invalid }

public sealed record RegistrationOutcome(RegistrationError Error, RegistrationResponse? Response);

public interface IProvisioningService
{
    Task<RegistrationOutcome> RegisterAsync(RegisterTenantRequest req, CancellationToken ct);
}
```

- [ ] **Step 3: `ProvisioningService`**

```csharp
using Microsoft.EntityFrameworkCore;
using TourKit.Api.Auth;
using TourKit.Api.Authz;
using TourKit.Api.Tenancy;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;

namespace TourKit.Api.Provisioning;

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

    public async Task<RegistrationOutcome> RegisterAsync(RegisterTenantRequest req, CancellationToken ct)
    {
        var slug = (req.Slug ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(slug) || string.IsNullOrWhiteSpace(req.CompanyName)
            || string.IsNullOrWhiteSpace(req.AdminEmail) || (req.AdminPassword?.Length ?? 0) < 8)
        {
            return new RegistrationOutcome(RegistrationError.Invalid, null);
        }

        if (await _db.Tenants.AnyAsync(t => t.Slug == slug && !t.IsDeleted, ct))
        {
            return new RegistrationOutcome(RegistrationError.SlugTaken, null);
        }

        // Lưu ý: các SaveChanges tuần tự (chưa bọc transaction — xem ghi chú atomicity ở plan).
        var tenant = new Tenant { Name = req.CompanyName.Trim(), Slug = slug };
        _db.Tenants.Add(tenant);
        await _db.SaveChangesAsync(ct);

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
        await _db.SaveChangesAsync(ct);

        var permIds = await _db.Permissions.Select(p => p.Id).ToListAsync(ct);
        foreach (var pid in permIds)
        {
            _db.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = pid });
        }

        _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
        await _db.SaveChangesAsync(ct);

        return new RegistrationOutcome(RegistrationError.None,
            new RegistrationResponse(tenant.Id, tenant.Slug, user.Id));
    }
}
```

- [ ] **Step 4: (test service viết chung với endpoint ở Task 2 — bỏ qua unit test riêng để tránh trùng)**

- [ ] **Step 5: Build** — `dotnet build` → 0/0.

---

### Task 2: Endpoint đăng ký + wire + test end-to-end

**Files:**
- Create: `Provisioning/RegistrationEndpoints.cs`
- Modify: `Program.cs`
- Test: `tests/TourKit.Tests/Provisioning/RegistrationEndpointTests.cs`

- [ ] **Step 1: Endpoint**

```csharp
namespace TourKit.Api.Provisioning;

public static class RegistrationEndpoints
{
    public static IEndpointRouteBuilder MapRegistrationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/registration", async (
            RegisterTenantRequest body, IProvisioningService svc, CancellationToken ct) =>
        {
            var outcome = await svc.RegisterAsync(body, ct);
            return outcome.Error switch
            {
                RegistrationError.None =>
                    Results.Created($"/api/v1/tenants/{outcome.Response!.TenantId}", outcome.Response),
                RegistrationError.SlugTaken =>
                    Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Slug đã được dùng."),
                _ => Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["Request"] = ["Thiếu thông tin hoặc mật khẩu < 8 ký tự."],
                }),
            };
        }).AllowAnonymous();

        return app;
    }
}
```

- [ ] **Step 2: Wire `Program.cs`** — thêm DI + map:

Thêm `builder.Services.AddScoped<IProvisioningService, ProvisioningService>();` (cạnh các AddScoped auth), `using TourKit.Api.Provisioning;`, và `app.MapRegistrationEndpoints();` (cạnh MapAuthEndpoints).

- [ ] **Step 3: Test end-to-end**

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Api.Provisioning;
using TourKit.Tests.Support;

namespace TourKit.Tests.Provisioning;

public class RegistrationEndpointTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public RegistrationEndpointTests(AuthTestFactory factory) => _factory = factory;

    private static RegisterTenantRequest Sample(string slug) =>
        new("Công ty " + slug, slug, $"admin@{slug}.com", "P@ssw0rd!", "Admin");

    [Fact]
    public async Task Register_then_admin_can_login_and_has_full_permissions()
    {
        var client = _factory.CreateClient();

        var reg = await client.PostAsJsonAsync("/api/v1/registration", Sample("newco"));
        Assert.Equal(HttpStatusCode.Created, reg.StatusCode);
        var created = await reg.Content.ReadFromJsonAsync<RegistrationResponse>();
        Assert.NotNull(created);

        // login bằng admin vừa tạo
        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("newco", "admin@newco.com", "P@ssw0rd!"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();

        // admin có quyền tạo tour-template (tour.create)
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        var create = await client.PostAsJsonAsync("/api/v1/tour-templates", new
        {
            Code = "T-1", Title = "Tour", TourType = (string?)null, TotalSlots = 10, ReservationHours = 24,
            PriceAdult = 1m, PriceChild = 1m, PriceChildSmall = 1m, PriceBaby = 0m, TermsNote = (string?)null,
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
    }

    [Fact]
    public async Task Duplicate_slug_returns_409()
    {
        var client = _factory.CreateClient();
        (await client.PostAsJsonAsync("/api/v1/registration", Sample("dupco"))).EnsureSuccessStatusCode();

        var again = await client.PostAsJsonAsync("/api/v1/registration", Sample("dupco"));
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Short_password_returns_400()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/v1/registration",
            new RegisterTenantRequest("C", "shortpw", "a@b.com", "123", "A"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
}
```

- [ ] **Step 4: Build + test** — `dotnet build` 0/0; `dotnet test` → tất cả xanh (26 total: 23 cũ + 3 mới).

---

## Self-Review

**Spec coverage:** self-registration → Tenant+admin+Admin role(đủ quyền)+gán → Task 1 ✅; endpoint 201/409/400 → Task 2 ✅; test register→login→quyền + slug trùng → Task 2 ✅.
**Ngoài phạm vi:** invite user, CRUD role/permission API, email verify, chọn plan (0b-4), bọc transaction (follow-up atomicity).
**Type consistency:** `RegisterTenantRequest`, `RegistrationResponse`, `RegistrationOutcome`/`RegistrationError`, `IProvisioningService.RegisterAsync`, `MapRegistrationEndpoints` — nhất quán Task 1→2.
**Rủi ro:** atomicity (đã ghi chú); slug chuẩn hóa lowercase để tránh trùng do hoa/thường; đăng ký anonymous nên không cần tenant context ban đầu (SetTenant sau khi tạo Tenant).
