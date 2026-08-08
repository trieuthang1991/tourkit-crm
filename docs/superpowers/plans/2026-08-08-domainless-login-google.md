# Domainless Login and Google OAuth Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove tenant/domain input from login and password recovery, enforce one globally unique email per company account, and add Google login plus Google-first company onboarding.

**Architecture:** Keep M-Travel as the owner of users, tenants, RBAC, cookies, and JWTs. Add normalized global email identity and provider-subject links, use a short-lived external cookie for Google, and convert every successful password/Google authentication into the same internal tenant-aware principal. Google users with a new email complete company onboarding before any database records are created.

**Tech Stack:** .NET 9, ASP.NET Core Razor Pages, ASP.NET Core Google authentication handler, EF Core 9, PostgreSQL/SQLite/SQL Server, xUnit, WebApplicationFactory.

## Global Constraints

- The canonical frontend is Razor Pages; do not extend the abandoned React frontend in `web/`.
- One normalized email belongs to exactly one user and one tenant across the entire database.
- Normalize user email with `Trim()` plus `ToUpperInvariant()`; preserve the trimmed original casing in `User.Email` for display.
- Keep `PasswordHash` required. Google-created accounts receive an independent 32-byte CSPRNG password that is hashed and immediately discarded.
- Never introduce a fixed/default password shared by Google accounts.
- Implement Google only; the external-login data model may remain provider-neutral.
- A new Google email must complete company name, slug, full name, and terms acceptance before provisioning.
- Missing Google credentials must hide the Google button without preventing application startup or password login.
- Never trust provider email, subject, or Google mode from posted form fields; read them from the authenticated external cookie.
- Preserve generic login/reset responses so anonymous users cannot enumerate accounts.
- Preserve internal `sub`, `tenant_id`, `email`, `name`, and `perm` claims for cookie and JWT flows.
- Follow strict TDD: write one behavior test, observe the expected failure, implement the minimum, then refactor while green.
- Before modifying every existing class/method, run GitNexus upstream impact. Warn before HIGH/CRITICAL edits and update every d=1 dependent.
- Before each task commit, run `gitnexus_detect_changes({scope: "staged"})`; stage and commit only files owned by the task.
- The worktree already contains unrelated user changes. Do not stage, revert, format, or rewrite them.

## Risk Gate Already Measured

- `User`: **CRITICAL** — 301 direct dependents, 754 impacted symbols, 17 modules, `SeedAsync` and `Create` execution flows.
- `LoginRequest`: **CRITICAL** — 36 direct dependents, 124 impacted symbols, 14 modules.
- `AppDbContext`: **CRITICAL** — 70 direct dependents, 195 impacted symbols.
- `UserAdminService`: **MEDIUM** — 11 direct dependents, 31 impacted symbols.
- `AuthService`, `CookieAuthService`, `PasswordResetService`, `ProvisioningService`, and the auth PageModels: graph risk LOW.

The implementation must add compatible properties rather than changing existing constructors, keep `AppDbContext`'s constructor unchanged, and run the complete .NET test suite before completion.

## File Map

### New production files

- `src/TourKit.Shared/Security/UserEmail.cs` — canonical email normalization.
- `src/TourKit.Shared/Entities/UserExternalLogin.cs` — tenant-scoped provider subject link.
- `src/TourKit.Infrastructure/Persistence/Configurations/UserExternalLoginConfiguration.cs` — foreign keys and unique indexes.
- `src/TourKit.Application/Auth/IUserIdentityStore.cs` — global identity lookup boundary.
- `src/TourKit.Infrastructure/Auth/UserIdentityStore.cs` — EF implementation that deliberately bypasses tenant filters only for identity resolution.
- `src/TourKit.Application/Auth/ExternalAuthContracts.cs` — provider-neutral identity/result records and status enum.
- `src/TourKit.Application/Auth/IExternalAuthService.cs` — external identity resolution contract.
- `src/TourKit.Infrastructure/Auth/ExternalAuthService.cs` — linked-user lookup, first-link-by-email, and onboarding decision.
- `src/TourKit.Api/Configuration/GoogleAuthOptions.cs` — typed Google enablement and credential readiness.
- `src/TourKit.Api/Auth/ExternalAuthDefaults.cs` — external cookie scheme name and lifetime.
- `src/TourKit.Api/Auth/GoogleIdentityReader.cs` — strict conversion of Google claims to `ExternalIdentity`.
- `src/TourKit.Infrastructure/Migrations/*_AddGlobalUserIdentity.cs` and `.Designer.cs` — EF-generated files whose numeric prefix is assigned by Task 1 Step 6.

### New test files

- `tests/TourKit.Tests/Auth/UserIdentityPersistenceTests.cs` — real SQLite uniqueness/index behavior.
- `tests/TourKit.Tests/Auth/PasswordResetServiceTests.cs` — email-only reset behavior with a capturing sender.
- `tests/TourKit.Tests/Auth/ExternalAuthServiceTests.cs` — Google identity resolution using the real EF store.
- `tests/TourKit.UnitTests/Auth/GoogleIdentityReaderTests.cs` — verified-claim parsing and rejection.
- `tests/TourKit.Tests/Provisioning/GoogleRegistrationTests.cs` — complete Google tenant provisioning and rollback-visible outcomes.
- `tests/TourKit.Tests/Support/StubAuthenticationService.cs` — test-only external-cookie boundary for Razor PageModel tests.
- `tests/TourKit.Tests/Web/GoogleLoginPageTests.cs` — challenge, callback, cookie cleanup, and local redirect behavior.
- `tests/TourKit.Tests/Web/GoogleOnboardingPageTests.cs` — server-owned Google email/subject and automatic internal sign-in.

### Existing production files to modify

- `src/TourKit.Shared/Entities/User.cs`
- `src/TourKit.Infrastructure/Persistence/AppDbContext.cs`
- `src/TourKit.Infrastructure/Persistence/Configurations/UserConfiguration.cs`
- `src/TourKit.Infrastructure/Migrations/AppDbContextModelSnapshot.cs`
- `src/TourKit.Application/Auth/AuthContracts.cs`
- `src/TourKit.Application/Auth/ICookieAuthService.cs`
- `src/TourKit.Application/Auth/IPasswordResetService.cs`
- `src/TourKit.Infrastructure/Auth/AuthService.cs`
- `src/TourKit.Infrastructure/Auth/CookieAuthService.cs`
- `src/TourKit.Infrastructure/Auth/PasswordResetService.cs`
- `src/TourKit.Application/Provisioning/RegistrationContracts.cs`
- `src/TourKit.Application/Provisioning/IProvisioningService.cs`
- `src/TourKit.Infrastructure/Provisioning/ProvisioningService.cs`
- `src/TourKit.Application/Admin/UserAdminService.cs`
- `src/TourKit.Api/Controllers/RegistrationController.cs`
- `src/TourKit.Api/Pages/Auth/Login.cshtml.cs`
- `src/TourKit.Api/Pages/Auth/Login.cshtml`
- `src/TourKit.Api/Pages/Auth/ForgotPassword.cshtml.cs`
- `src/TourKit.Api/Pages/Auth/ForgotPassword.cshtml`
- `src/TourKit.Api/Pages/Auth/Register.cshtml.cs`
- `src/TourKit.Api/Pages/Auth/Register.cshtml`
- `src/TourKit.Api/Configuration/OptionsStartup.cs`
- `src/TourKit.Api/Program.cs`
- `src/TourKit.Api/TourKit.Api.csproj`
- `src/TourKit.Api/appsettings.example.json`
- `README.md`

### Existing tests/helpers to modify

- `tests/TourKit.Tests/Auth/AuthEndpointTests.cs`
- `tests/TourKit.Tests/Auth/CookieAuthServiceTests.cs`
- `tests/TourKit.Tests/Provisioning/RegistrationEndpointTests.cs`
- `tests/TourKit.Tests/Billing/SubscriptionTests.cs`
- `tests/TourKit.UnitTests/Admin/UserAdminServiceTests.cs`
- `tests/TourKit.Tests/Support/AuthTestFactory.cs`
- `tests/TourKit.Tests/Web/RazorPagesSmokeTests.cs`
- Every integration test returned by `rg -l "new LoginRequest" tests --glob '*.cs'`; the exact 34-file list is captured in Task 2.

---

### Task 1: Global Email Identity Schema

**Files:**

- Create: `src/TourKit.Shared/Security/UserEmail.cs`
- Create: `src/TourKit.Shared/Entities/UserExternalLogin.cs`
- Create: `src/TourKit.Infrastructure/Persistence/Configurations/UserExternalLoginConfiguration.cs`
- Create: `tests/TourKit.Tests/Auth/UserIdentityPersistenceTests.cs`
- Modify: `src/TourKit.Shared/Entities/User.cs`
- Modify: `src/TourKit.Infrastructure/Persistence/AppDbContext.cs`
- Modify: `src/TourKit.Infrastructure/Persistence/Configurations/UserConfiguration.cs`
- Modify: `src/TourKit.Infrastructure/Migrations/AppDbContextModelSnapshot.cs`
- Generate: `src/TourKit.Infrastructure/Migrations/*_AddGlobalUserIdentity.cs` (the command in Step 6 assigns the numeric prefix)
- Generate: `src/TourKit.Infrastructure/Migrations/*_AddGlobalUserIdentity.Designer.cs` (the matching designer file)

**Interfaces:**

- Produces: `UserEmail.Normalize(string? value) -> string`.
- Produces: `User.NormalizedEmail : string`.
- Produces: `UserExternalLogin` with `TenantId`, `UserId`, `Provider`, `ProviderSubject`, and `ProviderEmail`.
- Database guarantees: unique `User.NormalizedEmail`, unique `(Provider, ProviderSubject)`, unique `(UserId, Provider)`.

- [ ] **Step 1: Run impact gates and report the CRITICAL risk before editing**

Run:

```powershell
npx gitnexus impact User --direction upstream --repo tourkit-crm --include-tests
npx gitnexus impact AppDbContext --direction upstream --repo tourkit-crm --include-tests
npx gitnexus impact UserConfiguration --direction upstream --repo tourkit-crm --include-tests
```

Confirm the report still identifies all d=1 seed/test constructors. Do not change the `User` or `AppDbContext` constructors.

- [ ] **Step 2: Write failing normalization and SQLite uniqueness tests**

Add tests equivalent to:

```csharp
[Theory]
[InlineData(" Admin@Example.Com ", "ADMIN@EXAMPLE.COM")]
[InlineData("sales@công-ty.vn", "SALES@CÔNG-TY.VN")]
public void Normalize_trims_and_uses_invariant_uppercase(string input, string expected)
    => Assert.Equal(expected, UserEmail.Normalize(input));

[Fact]
public async Task Normalized_email_is_unique_across_tenants()
{
    await using var connection = new SqliteConnection("Data Source=:memory:");
    await connection.OpenAsync();
    var tenant = new AmbientTenantContext();
    var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
    await using var db = new AppDbContext(options, tenant);
    await db.Database.EnsureCreatedAsync();

    var a = new Tenant { Name = "A", Slug = "a-company" };
    var b = new Tenant { Name = "B", Slug = "b-company" };
    db.Tenants.AddRange(a, b);
    await db.SaveChangesAsync();

    tenant.SetTenant(a.Id);
    db.Users.Add(new User { Email = "Admin@Example.com", FullName = "A", PasswordHash = "hash-a" });
    await db.SaveChangesAsync();

    tenant.SetTenant(b.Id);
    db.Users.Add(new User { Email = " admin@example.COM ", FullName = "B", PasswordHash = "hash-b" });
    await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
}
```

Add a second SQLite test that inserts two `UserExternalLogin` rows with the same `Provider="Google"` and `ProviderSubject="google-sub-1"` under different tenants and expects `DbUpdateException`.

- [ ] **Step 3: Run the focused tests and observe RED**

Run:

```powershell
dotnet test tests/TourKit.Tests/TourKit.Tests.csproj -c Release --filter FullyQualifiedName~UserIdentityPersistenceTests
```

Expected: compilation fails because `UserEmail`, `NormalizedEmail`, and `UserExternalLogin` do not exist.

- [ ] **Step 4: Implement automatic normalization and provider-link configuration**

Create the normalizer:

```csharp
namespace TourKit.Shared.Security;

public static class UserEmail
{
    public static string Normalize(string? value)
        => (value ?? string.Empty).Trim().ToUpperInvariant();
}
```

Add `NormalizedEmail` to `User` without changing its constructor or existing properties. Add `UserExternalLogin : BaseEntity, ITenantEntity`. In `AppDbContext.ApplyTenantAndTimestamps()`, before persistence, process only added/modified `User` entries:

```csharp
foreach (var entry in ChangeTracker.Entries<User>()
             .Where(e => e.State is EntityState.Added or EntityState.Modified))
{
    entry.Entity.Email = entry.Entity.Email.Trim();
    entry.Entity.NormalizedEmail = UserEmail.Normalize(entry.Entity.Email);
}
```

Add `DbSet<UserExternalLogin>`. Replace the old `(TenantId, Email)` unique index with a unique `NormalizedEmail` index, and configure the two external-login unique indexes plus `UserId` foreign key.

- [ ] **Step 5: Run focused tests and observe GREEN**

Run the focused command from Step 3. Expected: both SQLite uniqueness tests and normalization theory pass.

- [ ] **Step 6: Generate and harden the EF migration**

Run:

```powershell
dotnet ef migrations add AddGlobalUserIdentity --project src/TourKit.Infrastructure --startup-project src/TourKit.Api
```

Review the generated migration. It must:

1. add `NormalizedEmail` with a temporary empty default;
2. preflight duplicate `UPPER(TRIM(Email))` values and fail instead of merging/deleting users;
3. backfill `NormalizedEmail`;
4. drop the `(TenantId, Email)` unique index;
5. create the global normalized-email index;
6. create the external-login table and both unique indexes;
7. reverse those operations in `Down()`.

Use `ActiveProvider` branches for PostgreSQL, SQL Server, and SQLite SQL syntax. Do not touch any prior migration.

- [ ] **Step 7: Verify model and migration**

Run:

```powershell
dotnet build TourKit.sln -c Release
dotnet test tests/TourKit.Tests/TourKit.Tests.csproj -c Release --filter "FullyQualifiedName~UserIdentityPersistenceTests|FullyQualifiedName~TenantWriteIsolationTests|FullyQualifiedName~TenantReadIsolationTests"
```

Expected: build succeeds with no warnings; identity and tenancy tests pass.

- [ ] **Step 8: Detect scope and commit**

Run staged GitNexus change detection. Expected changes are limited to `User`, identity schema/configuration, `AppDbContext`, migration files, and the new persistence tests.

```powershell
git add -- src/TourKit.Shared/Security/UserEmail.cs src/TourKit.Shared/Entities/User.cs src/TourKit.Shared/Entities/UserExternalLogin.cs src/TourKit.Infrastructure/Persistence/AppDbContext.cs src/TourKit.Infrastructure/Persistence/Configurations/UserConfiguration.cs src/TourKit.Infrastructure/Persistence/Configurations/UserExternalLoginConfiguration.cs src/TourKit.Infrastructure/Migrations/*_AddGlobalUserIdentity*.cs src/TourKit.Infrastructure/Migrations/AppDbContextModelSnapshot.cs tests/TourKit.Tests/Auth/UserIdentityPersistenceTests.cs
git commit -m "feat(auth): enforce globally unique user email"
```

### Task 2: Domainless Password Login

**Files:**

- Create: `src/TourKit.Application/Auth/IUserIdentityStore.cs`
- Create: `src/TourKit.Infrastructure/Auth/UserIdentityStore.cs`
- Modify: `src/TourKit.Application/Auth/AuthContracts.cs`
- Modify: `src/TourKit.Application/Auth/ICookieAuthService.cs`
- Modify: `src/TourKit.Infrastructure/Auth/AuthService.cs`
- Modify: `src/TourKit.Infrastructure/Auth/CookieAuthService.cs`
- Modify: `src/TourKit.Api/Pages/Auth/Login.cshtml.cs`
- Modify: `src/TourKit.Api/Pages/Auth/Login.cshtml`
- Modify: `src/TourKit.Api/Program.cs`
- Modify: `tests/TourKit.Tests/Auth/AuthEndpointTests.cs`
- Modify: `tests/TourKit.Tests/Auth/CookieAuthServiceTests.cs`
- Modify: `tests/TourKit.Tests/Support/AuthTestFactory.cs`
- Modify the following LoginRequest callers:
  `tests/TourKit.Tests/Support/WorkflowKanbanTests.cs`,
  `tests/TourKit.Tests/Support/TransferReasonTests.cs`,
  `tests/TourKit.Tests/Support/PostCommentEndpointTests.cs`,
  `tests/TourKit.Tests/Support/MessageTemplateTests.cs`,
  `tests/TourKit.Tests/Support/CompanyProfileTests.cs`,
  `tests/TourKit.Tests/Support/ApprovalProcessTests.cs`,
  `tests/TourKit.Tests/Catalog/TourTemplateEndpointTests.cs`,
  `tests/TourKit.Tests/Catalog/CatalogExtrasTests.cs`,
  `tests/TourKit.Tests/Booking/TourTransferTests.cs`,
  `tests/TourKit.Tests/Booking/SeatFlowTests.cs`,
  `tests/TourKit.Tests/Sales/QuoteConvertTests.cs`,
  `tests/TourKit.Tests/Booking/OrderContractTests.cs`,
  `tests/TourKit.Tests/Booking/DepartureCloseTests.cs`,
  `tests/TourKit.Tests/Reports/TurnoverByDepartmentReportTests.cs`,
  `tests/TourKit.Tests/Booking/BookingEndpointTests.cs`,
  `tests/TourKit.Tests/Reports/KpiReportTests.cs`,
  `tests/TourKit.Tests/Reports/DebtReportTests.cs`,
  `tests/TourKit.Tests/Booking/BatchDepartureTests.cs`,
  `tests/TourKit.Tests/Provisioning/RegistrationEndpointTests.cs`,
  `tests/TourKit.Tests/Billing/SubscriptionTests.cs`,
  `tests/TourKit.Tests/Billing/SubscriptionGuardTests.cs`,
  `tests/TourKit.Tests/Providers/ProviderEndpointTests.cs`,
  `tests/TourKit.Tests/Providers/OrderCostTests.cs`,
  `tests/TourKit.Tests/Marketing/MarketingTests.cs`,
  `tests/TourKit.Tests/Api/CustomerStatsEndpointTests.cs`,
  `tests/TourKit.Tests/Api/CustomerSearchPagingTests.cs`,
  `tests/TourKit.Tests/Finance/ReceiptEndpointTests.cs`,
  `tests/TourKit.Tests/Api/CustomerFilterEndpointTests.cs`,
  `tests/TourKit.Tests/Finance/ReceiptApprovalTests.cs`,
  `tests/TourKit.Tests/Api/CustomerEndpointIsolationTests.cs`,
  `tests/TourKit.Tests/Crm/LeadEndpointTests.cs`,
  `tests/TourKit.Tests/Api/CustomerCrmProfileTests.cs`,
  `tests/TourKit.Tests/Commission/CommissionTests.cs`.

**Interfaces:**

- Changes: `LoginRequest(string Email, string Password)`.
- Changes: `ICookieAuthService.AuthenticateAsync(string email, string password)`.
- Produces: `ICookieAuthService.CreatePrincipalAsync(Guid userId)` for Google reuse.
- Produces: `IUserIdentityStore.FindByEmailAsync`, `FindByIdAsync`, `TenantIsActiveAsync`, and external-login methods used in later tasks.

- [ ] **Step 1: Run impact gates and report CRITICAL LoginRequest risk**

Run impact for `LoginRequest`, `AuthService`, `CookieAuthService`, `LoginModel`, and `Program`. Review all 36 d=1 `LoginRequest` callers before editing.

- [ ] **Step 2: Write the failing API and cookie behavior tests**

Change the focused API test to send only email/password and prove normalization:

```csharp
var login = await client.PostAsJsonAsync("/api/v1/auth/login",
    new LoginRequest($"  {email.ToUpperInvariant()}  ", password));
Assert.Equal(HttpStatusCode.OK, login.StatusCode);
```

Change the cookie test to:

```csharp
var principal = await svc.AuthenticateAsync($"  {email.ToUpperInvariant()}  ", password);
Assert.Equal(email, principal!.FindFirst("email")?.Value);
Assert.False(string.IsNullOrWhiteSpace(principal.FindFirst("tenant_id")?.Value));
```

Add a Razor smoke assertion that `/dang-nhap` does not contain `Input.TenantSlug` or “Mã doanh nghiệp”.

- [ ] **Step 3: Run focused tests and observe RED**

Run:

```powershell
dotnet test tests/TourKit.Tests/TourKit.Tests.csproj -c Release --filter "FullyQualifiedName~AuthEndpointTests|FullyQualifiedName~CookieAuthServiceTests|FullyQualifiedName~RazorPagesSmokeTests.Login_page"
```

Expected: compile/behavior failure because the old contract and Razor form still require tenant slug.

- [ ] **Step 4: Implement the identity store and domainless lookup**

Define the application boundary with these signatures:

```csharp
public interface IUserIdentityStore
{
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> FindByIdAsync(Guid userId, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task<bool> TenantIsActiveAsync(Guid tenantId, CancellationToken ct = default);
    Task<UserExternalLogin?> FindExternalAsync(string provider, string subject, CancellationToken ct = default);
    Task<bool> HasExternalAsync(Guid userId, string provider, CancellationToken ct = default);
    Task AddExternalAsync(UserExternalLogin login, CancellationToken ct = default);
}
```

Implement it with `IgnoreQueryFilters()` for user/external lookup and `UserEmail.Normalize(email)`. Do not set tenant context inside the store; auth/provisioning services own that transition.

Update password services to find by email, verify active user and active tenant, set `AmbientTenantContext`, then issue existing claims/tokens. Extract cookie principal creation into `CreatePrincipalAsync(Guid userId)` so password and Google flows cannot drift.

Register `IUserIdentityStore` in `Program.cs`.

- [ ] **Step 5: Remove tenant slug from the active login UI**

Delete `TenantSlug` from `LoginModel.InputModel`, remove `IHostEnvironment`, remove the development-prefill `OnGet`, call the new two-argument cookie service, and change the generic failure copy to “Email hoặc mật khẩu không đúng.” Remove the tenant field from `Login.cshtml`; keep email autofocus, password, remember-me, reset link, and registration link.

- [ ] **Step 6: Update all 36 LoginRequest callers mechanically**

For every exact file listed above, replace `new LoginRequest(slug, email, password)` with `new LoginRequest(email, password)` while preserving `slug` variables used for tenant seeding and assertions. Re-run:

```powershell
rg -n "new LoginRequest\([^,]+,[^,]+,[^\)]+\)" tests --glob '*.cs'
```

Expected: no three-argument LoginRequest construction remains.

- [ ] **Step 7: Run focused and complete auth tests**

```powershell
dotnet test tests/TourKit.Tests/TourKit.Tests.csproj -c Release --filter "FullyQualifiedName~Auth|FullyQualifiedName~RegistrationEndpointTests|FullyQualifiedName~SubscriptionTests"
```

Expected: all selected tests pass; tenant isolation claims remain unchanged.

- [ ] **Step 8: Detect scope and commit**

GitNexus staged detection must show the expected auth contract plus callers and no unrelated execution flows.

```powershell
$loginCallers = rg -l "new LoginRequest" tests --glob '*.cs'
git add -- src/TourKit.Application/Auth/AuthContracts.cs src/TourKit.Application/Auth/ICookieAuthService.cs src/TourKit.Application/Auth/IUserIdentityStore.cs src/TourKit.Infrastructure/Auth/AuthService.cs src/TourKit.Infrastructure/Auth/CookieAuthService.cs src/TourKit.Infrastructure/Auth/UserIdentityStore.cs src/TourKit.Api/Pages/Auth/Login.cshtml src/TourKit.Api/Pages/Auth/Login.cshtml.cs src/TourKit.Api/Program.cs tests/TourKit.Tests/Auth/AuthEndpointTests.cs tests/TourKit.Tests/Auth/CookieAuthServiceTests.cs tests/TourKit.Tests/Support/AuthTestFactory.cs tests/TourKit.Tests/Web/RazorPagesSmokeTests.cs $loginCallers
git commit -m "feat(auth): infer tenant from globally unique email"
```

### Task 3: Global Registration Uniqueness and Email-Only Reset

**Files:**

- Create: `tests/TourKit.Tests/Auth/PasswordResetServiceTests.cs`
- Modify: `src/TourKit.Application/Auth/IPasswordResetService.cs`
- Modify: `src/TourKit.Infrastructure/Auth/PasswordResetService.cs`
- Modify: `src/TourKit.Application/Provisioning/IProvisioningService.cs`
- Modify: `src/TourKit.Infrastructure/Provisioning/ProvisioningService.cs`
- Modify: `src/TourKit.Application/Admin/UserAdminService.cs`
- Modify: `src/TourKit.Api/Controllers/RegistrationController.cs`
- Modify: `src/TourKit.Api/Pages/Auth/Register.cshtml.cs`
- Modify: `src/TourKit.Api/Pages/Auth/ForgotPassword.cshtml.cs`
- Modify: `src/TourKit.Api/Pages/Auth/ForgotPassword.cshtml`
- Modify: `tests/TourKit.Tests/Provisioning/RegistrationEndpointTests.cs`
- Modify: `tests/TourKit.UnitTests/Admin/UserAdminServiceTests.cs`
- Modify: `tests/TourKit.Tests/Web/RazorPagesSmokeTests.cs`

**Interfaces:**

- Changes: `SendResetLinkAsync(string email, Func<string,string> buildUrl, CancellationToken ct = default)`.
- Adds: `RegistrationError.EmailTaken` and `RegistrationError.Conflict`.
- Changes: `UserAdminService` consumes `IUserIdentityStore` for global checks.

- [ ] **Step 1: Run impact gates**

Run upstream impact for `PasswordResetService`, `ProvisioningService`, `RegistrationError`, `UserAdminService`, `ForgotPasswordModel`, and `RegisterModel`. Report the MEDIUM UserAdminService dependents before changing its constructor.

- [ ] **Step 2: Write failing duplicate-email and reset tests**

Add registration behavior:

```csharp
[Fact]
public async Task Duplicate_email_in_different_company_returns_409()
{
    var client = _factory.CreateClient();
    await client.PostAsJsonAsync("/api/v1/registration", Sample("company-a"));
    var duplicate = Sample("company-b") with { AdminEmail = " ADMIN@COMPANY-A.COM " };
    var response = await client.PostAsJsonAsync("/api/v1/registration", duplicate);
    Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
}
```

Add a real `PasswordResetService` test with InMemory EF, `EphemeralDataProtectionProvider`, and a capturing `IEmailSender`; call `SendResetLinkAsync($"  {email.ToUpperInvariant()}  ", token => "/reset?token=" + token)` and assert exactly one message is captured. Add the non-existing-email case and assert zero messages without an exception.

Add Razor smoke assertions that `/quen-mat-khau` contains only the email field and no tenant slug/copy.

- [ ] **Step 3: Run tests and observe RED**

```powershell
dotnet test tests/TourKit.Tests/TourKit.Tests.csproj -c Release --filter "FullyQualifiedName~RegistrationEndpointTests|FullyQualifiedName~PasswordResetServiceTests|FullyQualifiedName~RazorPagesSmokeTests"
```

Expected: duplicate email is accepted and reset still requires tenant slug.

- [ ] **Step 4: Make provisioning globally unique and atomic**

Use `IUserIdentityStore.EmailExistsAsync` before creating a tenant. Refactor normal provisioning to construct tenant, user, role, role-permissions, user-role, and subscription with their client-generated Guid IDs, then call `SaveChangesAsync()` once. A single relational `SaveChanges` supplies transaction atomicity and still works with InMemory tests.

Map `EmailTaken` and final race `Conflict` to HTTP 409 and clear Vietnamese Razor messages. Keep password validation unchanged for normal registration.

- [ ] **Step 5: Enforce global uniqueness in member administration**

Inject `IUserIdentityStore` into `UserAdminService`. Replace the tenant-filtered `userRepo.AnyAsync(u => u.Email == email)` check with `identity.EmailExistsAsync(email)`. Extend the unit-test `NewService` factory with a focused fake that normalizes keys via `UserEmail.Normalize` and add a test proving an email owned by another tenant is rejected.

- [ ] **Step 6: Remove tenant from password recovery**

Change the reset interface/service/PageModel/view to email-only lookup. Keep the same non-enumerating success message. Continue including tenant ID inside the protected reset payload and setting `AmbientTenantContext` before password update/token revocation.

- [ ] **Step 7: Verify and commit**

```powershell
dotnet test tests/TourKit.UnitTests/TourKit.UnitTests.csproj -c Release --filter FullyQualifiedName~UserAdminServiceTests
dotnet test tests/TourKit.Tests/TourKit.Tests.csproj -c Release --filter "FullyQualifiedName~RegistrationEndpointTests|FullyQualifiedName~PasswordResetServiceTests|FullyQualifiedName~RazorPagesSmokeTests"
```

Run staged GitNexus detection, then:

```powershell
git add -- src/TourKit.Application/Auth/IPasswordResetService.cs src/TourKit.Application/Provisioning/IProvisioningService.cs src/TourKit.Application/Admin/UserAdminService.cs src/TourKit.Infrastructure/Auth/PasswordResetService.cs src/TourKit.Infrastructure/Provisioning/ProvisioningService.cs src/TourKit.Api/Controllers/RegistrationController.cs src/TourKit.Api/Pages/Auth/Register.cshtml.cs src/TourKit.Api/Pages/Auth/ForgotPassword.cshtml src/TourKit.Api/Pages/Auth/ForgotPassword.cshtml.cs tests/TourKit.Tests/Auth/PasswordResetServiceTests.cs tests/TourKit.Tests/Provisioning/RegistrationEndpointTests.cs tests/TourKit.Tests/Web/RazorPagesSmokeTests.cs tests/TourKit.UnitTests/Admin/UserAdminServiceTests.cs
git commit -m "feat(auth): enforce global email registration rules"
```

### Task 4: Google Authentication Foundation

**Files:**

- Create: `src/TourKit.Application/Auth/ExternalAuthContracts.cs`
- Create: `src/TourKit.Application/Auth/IExternalAuthService.cs`
- Create: `src/TourKit.Infrastructure/Auth/ExternalAuthService.cs`
- Create: `src/TourKit.Api/Configuration/GoogleAuthOptions.cs`
- Create: `src/TourKit.Api/Auth/ExternalAuthDefaults.cs`
- Create: `tests/TourKit.Tests/Auth/ExternalAuthServiceTests.cs`
- Modify: `src/TourKit.Api/Configuration/OptionsStartup.cs`
- Modify: `src/TourKit.Api/Program.cs`
- Modify: `src/TourKit.Api/TourKit.Api.csproj`

**Interfaces:**

- Produces: `ExternalIdentity(string Provider, string Subject, string Email, bool EmailVerified, string? DisplayName)`.
- Produces: `ExternalAuthOutcome(ExternalAuthStatus Status, Guid? UserId, string? Error)`.
- Produces: `IExternalAuthService.ResolveAsync(ExternalIdentity identity, CancellationToken ct = default)`.
- Produces: `GoogleAuthOptions.IsConfigured`.
- Schemes: main cookie unchanged, external cookie `tourkit_external`, Google scheme `Google` only when configured.

- [ ] **Step 1: Run impact gates**

Run upstream impact for `Program`, `OptionsStartup`, `CookieAuthService`, and `UserExternalLogin`. Review authentication pipeline dependents before editing `Program.cs`.

- [ ] **Step 2: Write failing external-resolution tests**

Cover these independent behaviors using a real InMemory `AppDbContext`, `AmbientTenantContext`, and `UserIdentityStore`:

```csharp
[Fact] public async Task Linked_subject_resolves_linked_user_even_if_provider_email_changed();
[Fact] public async Task New_subject_with_verified_existing_email_creates_one_link();
[Fact] public async Task New_verified_email_returns_needs_onboarding_without_writes();
[Fact] public async Task Unverified_email_is_rejected_without_writes();
[Fact] public async Task Existing_user_with_different_google_subject_is_rejected();
[Fact] public async Task Inactive_user_or_deleted_tenant_is_rejected();
```

For every test, assert the real database row count/state, not mock call counts.

- [ ] **Step 3: Run tests and observe RED**

```powershell
dotnet test tests/TourKit.Tests/TourKit.Tests.csproj -c Release --filter FullyQualifiedName~ExternalAuthServiceTests
```

Expected: compilation fails because the contracts/service do not exist.

- [ ] **Step 4: Implement provider-neutral resolution**

Use this status model:

```csharp
public enum ExternalAuthStatus { Authenticated, NeedsOnboarding, Rejected }

public sealed record ExternalIdentity(
    string Provider, string Subject, string Email, bool EmailVerified, string? DisplayName);

public sealed record ExternalAuthOutcome(
    ExternalAuthStatus Status, Guid? UserId = null, string? Error = null);
```

Resolution order is mandatory: validate verified email/subject → find `(provider, subject)` → validate linked user/tenant → otherwise find normalized email → if none, return onboarding → reject a different existing provider link → set tenant context, add the first link, save, return authenticated.

- [ ] **Step 5: Add Google options and conditional authentication schemes**

Add package:

```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.Google" Version="9.0.*" />
```

`GoogleAuthOptions` contains `Enabled`, `ClientId`, `ClientSecret`, and:

```csharp
public bool IsConfigured => Enabled
    && !string.IsNullOrWhiteSpace(ClientId)
    && !string.IsNullOrWhiteSpace(ClientSecret);
```

Always register the short-lived external cookie using `ExternalAuthDefaults.Scheme = "tourkit_external"`. Configure `ExpireTimeSpan = TimeSpan.FromMinutes(10)`, `SlidingExpiration = false`, `Cookie.HttpOnly = true`, `Cookie.Name = "tourkit_external"`, `Cookie.SameSite = SameSiteMode.Lax`, and the same development/production secure-policy split as the main cookie. Conditionally call `.AddGoogle(...)` only when configured. Set its `SignInScheme` to the external cookie and map `email_verified`. Preserve the existing smart policy selector, JWT scheme, and main cookie unchanged.

- [ ] **Step 6: Verify missing-config startup and service behavior**

```powershell
dotnet test tests/TourKit.Tests/TourKit.Tests.csproj -c Release --filter "FullyQualifiedName~ExternalAuthServiceTests|FullyQualifiedName~RazorPagesSmokeTests.Login_page"
dotnet build src/TourKit.Api/TourKit.Api.csproj -c Release
```

Expected: app test host starts without Google credentials and all resolution tests pass.

- [ ] **Step 7: Detect scope and commit**

```powershell
git add -- src/TourKit.Application/Auth/ExternalAuthContracts.cs src/TourKit.Application/Auth/IExternalAuthService.cs src/TourKit.Infrastructure/Auth/ExternalAuthService.cs src/TourKit.Api/Configuration/GoogleAuthOptions.cs src/TourKit.Api/Configuration/OptionsStartup.cs src/TourKit.Api/Auth/ExternalAuthDefaults.cs src/TourKit.Api/Program.cs src/TourKit.Api/TourKit.Api.csproj tests/TourKit.Tests/Auth/ExternalAuthServiceTests.cs
git commit -m "feat(auth): add Google identity resolution foundation"
```

### Task 5: Google Login for Existing Users

**Files:**

- Create: `src/TourKit.Api/Auth/GoogleIdentityReader.cs`
- Create: `tests/TourKit.UnitTests/Auth/GoogleIdentityReaderTests.cs`
- Create: `tests/TourKit.Tests/Support/StubAuthenticationService.cs`
- Create: `tests/TourKit.Tests/Web/GoogleLoginPageTests.cs`
- Modify: `src/TourKit.Api/Pages/Auth/Login.cshtml.cs`
- Modify: `src/TourKit.Api/Pages/Auth/Login.cshtml`
- Modify: `tests/TourKit.Tests/Web/RazorPagesSmokeTests.cs`

**Interfaces:**

- Produces: `GoogleIdentityReader.TryRead(ClaimsPrincipal principal, out ExternalIdentity? identity)`.
- Adds Razor handlers: `OnPostGoogleAsync` and `OnGetGoogleCallbackAsync`.
- Existing users finish with the existing main cookie and local redirect behavior.

- [ ] **Step 1: Run impact gates**

Run impact for `LoginModel`, `ICookieAuthService`, and `GoogleAuthOptions`. Report results before editing.

- [ ] **Step 2: Write failing claim-reader and rendering tests**

Add unit tests with literal claims proving a verified principal is accepted and missing subject/email/`email_verified=true` is rejected. Add two Razor smoke cases:

```csharp
Assert.DoesNotContain("Tiếp tục với Google", defaultConfigHtml, StringComparison.Ordinal);
Assert.Contains("Tiếp tục với Google", googleConfiguredHtml, StringComparison.Ordinal);
```

The configured factory supplies dummy non-secret Client ID/Secret through in-memory configuration; it must not call Google.

Use `StubAuthenticationService` only at the external-provider boundary and add PageModel behavior tests:

```csharp
[Fact] public async Task Google_post_returns_not_found_when_provider_is_disabled();
[Fact] public async Task Google_post_challenges_only_the_Google_scheme_when_configured();
[Fact] public async Task Linked_callback_signs_internal_cookie_and_clears_external_cookie();
[Fact] public async Task Rejected_callback_clears_external_cookie_and_shows_safe_error();
[Fact] public async Task Callback_replaces_external_return_url_with_tong_quan();
```

The stub records the real `IAuthenticationService` boundary inputs/results; assertions target the PageModel result and sign-in/sign-out state, never a mock element.

- [ ] **Step 3: Run tests and observe RED**

```powershell
dotnet test tests/TourKit.UnitTests/TourKit.UnitTests.csproj -c Release --filter FullyQualifiedName~GoogleIdentityReaderTests
dotnet test tests/TourKit.Tests/TourKit.Tests.csproj -c Release --filter "FullyQualifiedName~GoogleLoginPageTests|FullyQualifiedName~RazorPagesSmokeTests.Login_page"
```

- [ ] **Step 4: Implement strict Google claim parsing**

Read `ClaimTypes.NameIdentifier`, `ClaimTypes.Email`, display name, and mapped `email_verified`. Return false unless subject/email are non-empty and verification equals `true` case-insensitively. Construct provider `Google` exactly once in the reader.

- [ ] **Step 5: Implement login challenge and callback**

`OnPostGoogleAsync` must return 404 when Google is not configured. Otherwise challenge `GoogleDefaults.AuthenticationScheme` with an `AuthenticationProperties.RedirectUri` pointing to `OnGetGoogleCallbackAsync` and a validated local `returnUrl`.

The callback must:

1. authenticate the external cookie;
2. parse verified identity;
3. call `IExternalAuthService.ResolveAsync`;
4. redirect `NeedsOnboarding` to `/Auth/Register` while retaining the external cookie;
5. for `Authenticated`, call `ICookieAuthService.CreatePrincipalAsync(userId)`, sign into the main cookie, delete external cookie, and local-redirect;
6. for terminal errors, delete external cookie and show a generic safe error.

Add an accessible separator and Google button to `Login.cshtml` only when `Model.GoogleEnabled` is true.

- [ ] **Step 6: Verify and commit**

```powershell
dotnet test tests/TourKit.UnitTests/TourKit.UnitTests.csproj -c Release --filter FullyQualifiedName~GoogleIdentityReaderTests
dotnet test tests/TourKit.Tests/TourKit.Tests.csproj -c Release --filter "FullyQualifiedName~GoogleLoginPageTests|FullyQualifiedName~CookieAuthServiceTests|FullyQualifiedName~RazorPagesSmokeTests"
```

Run staged GitNexus detection, then:

```powershell
git add -- src/TourKit.Api/Auth/GoogleIdentityReader.cs src/TourKit.Api/Pages/Auth/Login.cshtml src/TourKit.Api/Pages/Auth/Login.cshtml.cs tests/TourKit.UnitTests/Auth/GoogleIdentityReaderTests.cs tests/TourKit.Tests/Support/StubAuthenticationService.cs tests/TourKit.Tests/Web/GoogleLoginPageTests.cs tests/TourKit.Tests/Web/RazorPagesSmokeTests.cs
git commit -m "feat(auth): add Google login for existing users"
```

### Task 6: Google-First Company Onboarding

**Files:**

- Create: `tests/TourKit.Tests/Provisioning/GoogleRegistrationTests.cs`
- Create: `tests/TourKit.Tests/Web/GoogleOnboardingPageTests.cs`
- Use: `tests/TourKit.Tests/Support/StubAuthenticationService.cs`
- Modify: `src/TourKit.Application/Provisioning/RegistrationContracts.cs`
- Modify: `src/TourKit.Application/Provisioning/IProvisioningService.cs`
- Modify: `src/TourKit.Infrastructure/Provisioning/ProvisioningService.cs`
- Modify: `src/TourKit.Api/Pages/Auth/Register.cshtml.cs`
- Modify: `src/TourKit.Api/Pages/Auth/Register.cshtml`

**Interfaces:**

- Adds: `RegisterGoogleTenantRequest(string CompanyName, string Slug, string AdminEmail, string AdminFullName, string Provider, string ProviderSubject)`.
- Adds: `IProvisioningService.RegisterGoogleAsync(RegisterGoogleTenantRequest req)`.
- Google provisioning returns the existing `RegistrationResponse`, including `AdminUserId` for internal principal creation.

- [ ] **Step 1: Run impact gates**

Run impact for `ProvisioningService`, `IProvisioningService`, `RegisterModel`, `RegisterTenantRequest`, and `RegistrationResponse`. Review d=1 controller, seeder, billing, and registration test callers.

- [ ] **Step 2: Write failing Google provisioning tests**

Add integration tests that call the real provisioning service and assert:

```csharp
[Fact] public async Task Google_registration_creates_tenant_admin_permissions_subscription_and_external_link();
[Fact] public async Task Google_registration_uses_nonempty_unrecoverable_random_password_hash();
[Fact] public async Task Duplicate_email_or_slug_returns_conflict_without_partial_tenant();
```

For the password assertion, verify the stored hash is non-empty and does not verify against a known fixed value such as `"Google@123"`; do not expose or capture the random plaintext.

Add PageModel tests using `StubAuthenticationService` that supplies a complete external principal. Post an `Input.AdminEmail` different from the claim and assert the created user uses the claim email. Add an expired/missing external-cookie test and assert no tenant is created.

- [ ] **Step 3: Run tests and observe RED**

```powershell
dotnet test tests/TourKit.Tests/TourKit.Tests.csproj -c Release --filter "FullyQualifiedName~GoogleRegistrationTests|FullyQualifiedName~GoogleOnboardingPageTests"
```

- [ ] **Step 4: Implement Google provisioning with one atomic save**

Generate the unguessable password as:

```csharp
var passwordHash = _hasher.Hash(
    Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
```

Do not assign the random plaintext to an entity, response, or log field. Build the tenant, user, Admin role, all permission links, default subscription, and `UserExternalLogin` before a single `SaveChangesAsync`. Reuse a private provisioning core shared by normal and Google registration so the two flows cannot drift. Map precheck and unique-race failures to `EmailTaken`, `SlugTaken`, or `Conflict` without retaining partial rows.

- [ ] **Step 5: Implement server-owned onboarding state**

On GET, authenticate the external cookie, parse it with `GoogleIdentityReader`, set `IsGoogleRegistration=true`, display the verified email read-only, and prefill full name. On POST, authenticate and parse the external cookie again; never use a hidden email/subject as authority.

For Google mode, require company name, slug, full name, and terms but not password. For normal mode, preserve the existing password requirement. After successful Google provisioning, create the internal principal from `AdminUserId`, sign into the main cookie, delete the external cookie, and redirect to `/tong-quan`.

If the external cookie is absent/invalid, delete any stale external state and redirect to login with a safe message.

- [ ] **Step 6: Render the two registration modes**

In Google mode:

- show “Hoàn tất đăng ký với Google”;
- show the verified email as read-only text/input;
- omit the password input and password validation span;
- retain company name, slug, full name, and terms;
- submit to the same PageModel, which determines mode from the external cookie.

In normal mode, preserve all current fields and behavior.

- [ ] **Step 7: Verify and commit**

```powershell
dotnet test tests/TourKit.Tests/TourKit.Tests.csproj -c Release --filter "FullyQualifiedName~GoogleRegistrationTests|FullyQualifiedName~GoogleOnboardingPageTests|FullyQualifiedName~RegistrationEndpointTests|FullyQualifiedName~SubscriptionTests|FullyQualifiedName~RazorPagesSmokeTests"
```

Run staged GitNexus detection. Expected affected flow is registration/provisioning only, with auth principal issuance as a direct consumer.

```powershell
git add -- src/TourKit.Application/Provisioning/RegistrationContracts.cs src/TourKit.Application/Provisioning/IProvisioningService.cs src/TourKit.Infrastructure/Provisioning/ProvisioningService.cs src/TourKit.Api/Pages/Auth/Register.cshtml src/TourKit.Api/Pages/Auth/Register.cshtml.cs tests/TourKit.Tests/Provisioning/GoogleRegistrationTests.cs tests/TourKit.Tests/Web/GoogleOnboardingPageTests.cs
git commit -m "feat(auth): onboard new companies through Google"
```

### Task 7: Configuration, Documentation, and Full Verification

**Files:**

- Modify: `src/TourKit.Api/appsettings.example.json`
- Modify: `README.md`
- Verify: `tests/TourKit.Tests/Ai/AiConfigurationTests.cs` (no source change expected)

**Interfaces:**

- Documents environment keys `Authentication__Google__Enabled`, `Authentication__Google__ClientId`, and `Authentication__Google__ClientSecret`.
- Documents Google Console callback URI `/signin-google` for each HTTPS environment.

- [ ] **Step 1: Run impact and inspect current config contract**

Run impact for `GoogleAuthOptions` and `OptionsStartup`. Read the existing config parity test before editing it; do not add a source-text assertion when runtime binding can be tested.

- [ ] **Step 2: Add safe example configuration and operator instructions**

Add this non-secret shape to `appsettings.example.json`:

```json
"Authentication": {
  "Google": {
    "Enabled": false,
    "ClientId": "",
    "ClientSecret": ""
  }
}
```

Document environment-variable setup, Google Console redirect URI, disabled fallback behavior, and the migration duplicate-email preflight. Never copy local credentials into tracked files.

- [ ] **Step 3: Run focused security and auth tests**

```powershell
dotnet test tests/TourKit.UnitTests/TourKit.UnitTests.csproj -c Release --filter "FullyQualifiedName~GoogleIdentityReaderTests|FullyQualifiedName~UserAdminServiceTests"
dotnet test tests/TourKit.Tests/TourKit.Tests.csproj -c Release --filter "FullyQualifiedName~Auth|FullyQualifiedName~Registration|FullyQualifiedName~Google|FullyQualifiedName~Tenant|FullyQualifiedName~RazorPagesSmokeTests"
```

- [ ] **Step 4: Run full repository verification**

```powershell
dotnet build TourKit.sln -c Release
dotnet test TourKit.sln -c Release --no-build
git diff --check
```

Expected: build succeeds without warnings; all architecture, unit, and integration tests pass; diff check is clean.

- [ ] **Step 5: Verify migration on a disposable SQLite database**

Use an explicit workspace-local disposable path, not the configured development database:

```powershell
$migrationDb = Resolve-Path '.\artifacts' -ErrorAction SilentlyContinue
if (-not $migrationDb) { New-Item -ItemType Directory -Path '.\artifacts' | Out-Null }
$env:Database__Provider = 'Sqlite'
$env:ConnectionStrings__Default = 'Data Source=artifacts/domainless-login-migration-smoke.db'
dotnet ef database update --project src/TourKit.Infrastructure --startup-project src/TourKit.Api
```

Inspect that `Users.NormalizedEmail` and `UserExternalLogins` exist and their unique indexes are present. Remove only the explicit `artifacts/domainless-login-migration-smoke.db` file after resolving and confirming it is inside the repository `artifacts` directory.

```powershell
$repoRoot = (Resolve-Path '.').Path
$artifactRoot = (Resolve-Path '.\artifacts').Path
$smokeDb = (Resolve-Path '.\artifacts\domainless-login-migration-smoke.db').Path
if (-not $smokeDb.StartsWith($artifactRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to remove migration DB outside $artifactRoot"
}
Remove-Item -LiteralPath $smokeDb
Remove-Item Env:Database__Provider
Remove-Item Env:ConnectionStrings__Default
```

- [ ] **Step 6: Final GitNexus scope audit**

Run:

```text
gitnexus_detect_changes({scope: "all", repo: "tourkit-crm"})
```

Confirm expected changed symbols only: user identity schema, auth/password reset, provisioning/admin uniqueness, Google handlers/options, auth Razor pages, tests, and docs. Revisit every d=1 dependent reported for `User`, `LoginRequest`, and `AppDbContext`.

- [ ] **Step 7: Commit final configuration/docs**

```powershell
git add -- src/TourKit.Api/appsettings.example.json README.md
git commit -m "docs(auth): document Google login configuration"
```

- [ ] **Step 8: Refresh GitNexus after the final commit**

Check `.gitnexus/meta.json` for `stats.embeddings`. It is currently `0`, so run:

```powershell
npx gitnexus analyze
```

If embeddings become nonzero before execution, use `npx gitnexus analyze --embeddings` instead.
