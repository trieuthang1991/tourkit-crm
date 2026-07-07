# Phase 0a — Nền móng dữ liệu Multi-tenant — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **BẮT BUỘC ĐỌC TRƯỚC:** `docs/conventions/backend-conventions.md`. Mọi code trong plan phải tuân thủ nó (3 luật vàng, tenancy §4, EF §5). File `.editorconfig` + `Directory.Build.props` ở root sẽ ép tự động.

**Goal:** Dựng solution .NET và tầng dữ liệu EF Core đảm bảo mọi truy vấn/ghi bị cô lập theo `TenantId` một cách tự động, có test chứng minh không lộ dữ liệu chéo giữa các tenant.

**Architecture:** Modular monolith .NET 10. `TourKit.Shared` chứa kernel (base entity, tenant abstraction). `TourKit.Infrastructure` chứa `AppDbContext` áp Global Query Filter cho mọi `ITenantEntity` (đọc bị lọc theo tenant) và interceptor trong `SaveChanges` (ghi tự gán tenant + chặn sửa/xóa chéo tenant). `TourKit.Api` là composition root. Test dùng EF Core InMemory để chứng minh cô lập.

**Tech Stack:** .NET 10, EF Core 10 (SqlServer + InMemory), xUnit, SQL Server LocalDB (dev), ASP.NET Core Minimal API + Mvc.Testing.

---

## File Structure

```
TourKit.sln
src/
  TourKit.Shared/
    TourKit.Shared.csproj
    Entities/BaseEntity.cs            # Id, timestamps, soft-delete
    Entities/ITenantEntity.cs         # marker: Guid TenantId
    Tenancy/ITenantContext.cs         # TenantId hiện tại của request
  TourKit.Infrastructure/
    TourKit.Infrastructure.csproj
    Entities/Tenant.cs                # bảng hệ thống (KHÔNG có TenantId)
    Entities/Customer.cs              # entity mẫu có TenantId (dùng chứng minh cô lập)
    Persistence/AppDbContext.cs       # global filter + save interceptor
  TourKit.Api/
    TourKit.Api.csproj
    Tenancy/HttpTenantContext.cs      # resolve tenant từ header X-Tenant-Id (JWT sẽ thay ở Phase 0b)
    Program.cs                        # DI + endpoint /customers
tests/
  TourKit.Tests/
    TourKit.Tests.csproj
    Support/TestTenantContext.cs      # ITenantContext gán tay cho test
    Support/TestDb.cs                 # factory tạo AppDbContext InMemory
    Tenancy/TenantReadIsolationTests.cs
    Tenancy/TenantWriteIsolationTests.cs
    Api/CustomerEndpointIsolationTests.cs
```

**Nguyên tắc quyết định:** Tenancy là cross-cutting nên nằm ở Shared/Infrastructure (không tạo project `TourKit.Tenancy` riêng ở giai đoạn này — YAGNI). Các module nghiệp vụ (`Catalog`, `Booking`, `Finance`) sẽ được tạo thành project riêng ở các phase sau. `Customer` đặt tạm ở Infrastructure làm entity chứng minh cô lập; sẽ chuyển sang module `Booking` khi tạo module đó.

---

### Task 1: Scaffold solution & projects

**Files:**
- Create: `TourKit.sln`, `src/TourKit.Shared/`, `src/TourKit.Infrastructure/`, `src/TourKit.Api/`, `tests/TourKit.Tests/`

- [ ] **Step 1: Tạo solution và các project**

Chạy trong thư mục `E:/AI/TourKit`:

```bash
dotnet new sln -n TourKit
dotnet new classlib -n TourKit.Shared -o src/TourKit.Shared -f net10.0
dotnet new classlib -n TourKit.Infrastructure -o src/TourKit.Infrastructure -f net10.0
dotnet new web -n TourKit.Api -o src/TourKit.Api -f net10.0
dotnet new xunit -n TourKit.Tests -o tests/TourKit.Tests -f net10.0
```

- [ ] **Step 2: Xóa file mẫu thừa & thêm project vào solution**

```bash
rm -f src/TourKit.Shared/Class1.cs src/TourKit.Infrastructure/Class1.cs tests/TourKit.Tests/UnitTest1.cs
dotnet sln add src/TourKit.Shared src/TourKit.Infrastructure src/TourKit.Api tests/TourKit.Tests
```

- [ ] **Step 3: Thiết lập tham chiếu giữa các project**

```bash
dotnet add src/TourKit.Infrastructure reference src/TourKit.Shared
dotnet add src/TourKit.Api reference src/TourKit.Infrastructure src/TourKit.Shared
dotnet add tests/TourKit.Tests reference src/TourKit.Api src/TourKit.Infrastructure src/TourKit.Shared
```

- [ ] **Step 4: Cài NuGet packages**

```bash
dotnet add src/TourKit.Infrastructure package Microsoft.EntityFrameworkCore
dotnet add src/TourKit.Infrastructure package Microsoft.EntityFrameworkCore.SqlServer
dotnet add src/TourKit.Api package Microsoft.EntityFrameworkCore.Design
dotnet add tests/TourKit.Tests package Microsoft.EntityFrameworkCore.InMemory
dotnet add tests/TourKit.Tests package Microsoft.AspNetCore.Mvc.Testing
```

- [ ] **Step 5: Xác nhận enforcement đã bật**

`.editorconfig` và `Directory.Build.props` đã có sẵn ở root repo (xem `docs/conventions/backend-conventions.md §10`) — **không xóa**. Chúng tự áp cho mọi project vừa tạo: `net10.0`, `Nullable=enable`, `TreatWarningsAsErrors=true`, .NET analyzers. Không cần đặt `<TargetFramework>`/`<Nullable>` trong từng `.csproj` nữa (đã kế thừa); nếu template sinh sẵn, để nguyên cũng không sao.

- [ ] **Step 6: Build để xác nhận scaffold + enforcement OK**

Run: `dotnet build`
Expected: Build succeeded, 0 Warning, 0 Error. (Nếu có warning → build fail do `TreatWarningsAsErrors`; sửa cho sạch trước khi đi tiếp — đây là chủ đích.)

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "chore: scaffold TourKit solution (shared/infra/api/tests)"
```

---

### Task 2: Kernel — BaseEntity, ITenantEntity, ITenantContext

**Files:**
- Create: `src/TourKit.Shared/Entities/BaseEntity.cs`
- Create: `src/TourKit.Shared/Entities/ITenantEntity.cs`
- Create: `src/TourKit.Shared/Tenancy/ITenantContext.cs`
- Test: `tests/TourKit.Tests/Support/TestTenantContext.cs`

- [ ] **Step 1: Viết `ITenantContext`**

Create `src/TourKit.Shared/Tenancy/ITenantContext.cs`:

```csharp
namespace TourKit.Shared.Tenancy;

/// <summary>Tenant của request hiện tại. Được resolve từ JWT (Phase 0b) hoặc header (tạm thời).</summary>
public interface ITenantContext
{
    Guid TenantId { get; }
    bool HasTenant { get; }
}
```

- [ ] **Step 2: Viết `ITenantEntity`**

Create `src/TourKit.Shared/Entities/ITenantEntity.cs`:

```csharp
namespace TourKit.Shared.Entities;

/// <summary>Đánh dấu entity thuộc về một tenant. Mọi bảng nghiệp vụ phải implement.</summary>
public interface ITenantEntity
{
    Guid TenantId { get; set; }
}
```

- [ ] **Step 3: Viết `BaseEntity`**

Create `src/TourKit.Shared/Entities/BaseEntity.cs`:

```csharp
namespace TourKit.Shared.Entities;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
```

- [ ] **Step 4: Viết `TestTenantContext` (test support)**

Create `tests/TourKit.Tests/Support/TestTenantContext.cs`:

```csharp
using TourKit.Shared.Tenancy;

namespace TourKit.Tests.Support;

public sealed class TestTenantContext : ITenantContext
{
    public Guid TenantId { get; set; }
    public bool HasTenant => TenantId != Guid.Empty;
}
```

- [ ] **Step 5: Build**

Run: `dotnet build`
Expected: Build succeeded, 0 Error.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat(shared): thêm BaseEntity, ITenantEntity, ITenantContext"
```

---

### Task 3: Entities + AppDbContext (chưa có filter) — test đọc/ghi cơ bản

**Files:**
- Create: `src/TourKit.Infrastructure/Entities/Tenant.cs`
- Create: `src/TourKit.Infrastructure/Entities/Customer.cs`
- Create: `src/TourKit.Infrastructure/Persistence/AppDbContext.cs`
- Create: `tests/TourKit.Tests/Support/TestDb.cs`
- Test: `tests/TourKit.Tests/Tenancy/TenantReadIsolationTests.cs`

- [ ] **Step 1: Viết entity `Tenant` (bảng hệ thống, KHÔNG tenant)**

Create `src/TourKit.Infrastructure/Entities/Tenant.cs`:

```csharp
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Entities;

public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}
```

- [ ] **Step 2: Viết entity `Customer` (có TenantId)**

Create `src/TourKit.Infrastructure/Entities/Customer.cs`:

```csharp
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Entities;

public class Customer : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
}
```

- [ ] **Step 3: Viết `AppDbContext` (chưa có filter — thêm ở Task 4)**

Create `src/TourKit.Infrastructure/Persistence/AppDbContext.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Entities;
using TourKit.Shared.Tenancy;

namespace TourKit.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly ITenantContext _tenant;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Customer> Customers => Set<Customer>();
}
```

- [ ] **Step 4: Viết `TestDb` factory (InMemory)**

Create `tests/TourKit.Tests/Support/TestDb.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Tenancy;

namespace TourKit.Tests.Support;

public static class TestDb
{
    /// <summary>Tạo AppDbContext InMemory. Cùng dbName = cùng "database" (chia sẻ dữ liệu giữa các context).</summary>
    public static AppDbContext Create(ITenantContext tenant, string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .EnableSensitiveDataLogging()
            .Options;
        return new AppDbContext(options, tenant);
    }
}
```

- [ ] **Step 5: Viết test đọc cơ bản (chưa kiểm tra cô lập)**

Create `tests/TourKit.Tests/Tenancy/TenantReadIsolationTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Entities;
using TourKit.Tests.Support;
using Xunit;

namespace TourKit.Tests.Tenancy;

public class TenantReadIsolationTests
{
    [Fact]
    public async Task Can_add_and_read_customer()
    {
        var tenant = new TestTenantContext { TenantId = Guid.NewGuid() };
        var db = TestDb.Create(tenant, nameof(Can_add_and_read_customer));

        db.Customers.Add(new Customer { TenantId = tenant.TenantId, FullName = "Nguyen Van A" });
        await db.SaveChangesAsync();

        var count = await db.Customers.CountAsync();
        Assert.Equal(1, count);
    }
}
```

- [ ] **Step 6: Chạy test — kỳ vọng PASS**

Run: `dotnet test --filter Can_add_and_read_customer`
Expected: Passed! 1 test.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat(infra): Tenant/Customer entities + AppDbContext + test đọc cơ bản"
```

---

### Task 4: Global Query Filter — cô lập ĐỌC theo tenant

**Files:**
- Modify: `src/TourKit.Infrastructure/Persistence/AppDbContext.cs`
- Test: `tests/TourKit.Tests/Tenancy/TenantReadIsolationTests.cs`

- [ ] **Step 1: Viết test cô lập đọc (FAIL trước)**

Thêm vào `tests/TourKit.Tests/Tenancy/TenantReadIsolationTests.cs`:

```csharp
    [Fact]
    public async Task Tenant_only_sees_its_own_customers()
    {
        var dbName = nameof(Tenant_only_sees_its_own_customers);
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        // Seed: context của tenant A thêm khách của cả A và B (giả lập dữ liệu lẫn lộn ở tầng DB)
        var seedCtx = new TestTenantContext { TenantId = tenantA };
        using (var db = TestDb.Create(seedCtx, dbName))
        {
            db.Customers.Add(new Customer { TenantId = tenantA, FullName = "A-1" });
            db.Customers.Add(new Customer { TenantId = tenantB, FullName = "B-1" });
            await db.SaveChangesAsync();
        }

        // Đọc bằng context của tenant B — chỉ được thấy khách của B
        var ctxB = new TestTenantContext { TenantId = tenantB };
        using (var db = TestDb.Create(ctxB, dbName))
        {
            var names = await db.Customers.Select(c => c.FullName).ToListAsync();
            Assert.Equal(new[] { "B-1" }, names);
        }
    }
```

- [ ] **Step 2: Chạy test — kỳ vọng FAIL**

Run: `dotnet test --filter Tenant_only_sees_its_own_customers`
Expected: FAIL — trả về cả "A-1" và "B-1" (chưa có filter).

- [ ] **Step 3: Thêm Global Query Filter vào `AppDbContext`**

Sửa `src/TourKit.Infrastructure/Persistence/AppDbContext.cs` — thêm `using` và override `OnModelCreating`, thêm 2 helper. File đầy đủ:

```csharp
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Entities;
using TourKit.Shared.Entities;
using TourKit.Shared.Tenancy;

namespace TourKit.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    private readonly ITenantContext _tenant;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantContext tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var filter = BuildTenantFilterMethod
                    .MakeGenericMethod(entityType.ClrType)
                    .Invoke(this, null);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter((LambdaExpression)filter!);
            }
        }
    }

    private static readonly System.Reflection.MethodInfo BuildTenantFilterMethod =
        typeof(AppDbContext).GetMethod(nameof(BuildTenantFilter),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

    private LambdaExpression BuildTenantFilter<TEntity>() where TEntity : class, ITenantEntity
    {
        // Đóng gói tham chiếu tới _tenant.TenantId — EF Core đánh giá lại mỗi truy vấn.
        Expression<Func<TEntity, bool>> filter = e => e.TenantId == _tenant.TenantId;
        return filter;
    }
}
```

- [ ] **Step 4: Chạy lại cả 2 test đọc — kỳ vọng PASS**

Run: `dotnet test --filter TenantReadIsolationTests`
Expected: Passed! 2 tests.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(infra): global query filter cô lập đọc theo TenantId"
```

---

### Task 5: SaveChanges interceptor — tự gán tenant + timestamps khi GHI

**Files:**
- Modify: `src/TourKit.Infrastructure/Persistence/AppDbContext.cs`
- Test: `tests/TourKit.Tests/Tenancy/TenantWriteIsolationTests.cs`

- [ ] **Step 1: Viết test tự-gán-tenant (FAIL trước)**

Create `tests/TourKit.Tests/Tenancy/TenantWriteIsolationTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Entities;
using TourKit.Tests.Support;
using Xunit;

namespace TourKit.Tests.Tenancy;

public class TenantWriteIsolationTests
{
    [Fact]
    public async Task Insert_auto_assigns_current_tenant()
    {
        var tenantId = Guid.NewGuid();
        var ctx = new TestTenantContext { TenantId = tenantId };
        using var db = TestDb.Create(ctx, nameof(Insert_auto_assigns_current_tenant));

        // Cố tình KHÔNG set TenantId
        var customer = new Customer { FullName = "No-Tenant-Set" };
        db.Customers.Add(customer);
        await db.SaveChangesAsync();

        Assert.Equal(tenantId, customer.TenantId);
        Assert.NotEqual(default, customer.CreatedAt);
    }
}
```

- [ ] **Step 2: Chạy test — kỳ vọng FAIL**

Run: `dotnet test --filter Insert_auto_assigns_current_tenant`
Expected: FAIL — `customer.TenantId` là `Guid.Empty`.

- [ ] **Step 3: Thêm interceptor vào `AppDbContext`**

Thêm vào `src/TourKit.Infrastructure/Persistence/AppDbContext.cs` (bên trong class, thêm `using` nếu thiếu):

```csharp
    public override int SaveChanges()
    {
        ApplyTenantAndTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyTenantAndTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyTenantAndTimestamps()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.TenantId = _tenant.TenantId;
                    break;
                case EntityState.Modified:
                case EntityState.Deleted:
                    if (entry.Entity.TenantId != _tenant.TenantId)
                        throw new InvalidOperationException(
                            $"Chặn thao tác chéo tenant: entity thuộc {entry.Entity.TenantId}, request là {_tenant.TenantId}.");
                    break;
            }
        }

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = now;
            else if (entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }
    }
```

Lưu ý: cần `using Microsoft.EntityFrameworkCore;` (đã có) và `using TourKit.Shared.Entities;` (đã có ở Task 4).

- [ ] **Step 4: Chạy test — kỳ vọng PASS**

Run: `dotnet test --filter Insert_auto_assigns_current_tenant`
Expected: Passed! 1 test.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(infra): SaveChanges tự gán TenantId + timestamps khi ghi"
```

---

### Task 6: Chặn sửa/xóa chéo tenant

**Files:**
- Test: `tests/TourKit.Tests/Tenancy/TenantWriteIsolationTests.cs`

- [ ] **Step 1: Viết test chặn sửa chéo tenant (dùng code đã có ở Task 5)**

Thêm vào `tests/TourKit.Tests/Tenancy/TenantWriteIsolationTests.cs`:

```csharp
    [Fact]
    public async Task Cross_tenant_update_is_blocked()
    {
        var dbName = nameof(Cross_tenant_update_is_blocked);
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        // tenant A tạo 1 khách
        var ctxA = new TestTenantContext { TenantId = tenantA };
        Guid customerId;
        using (var db = TestDb.Create(ctxA, dbName))
        {
            var c = new Customer { FullName = "A-owned" };
            db.Customers.Add(c);
            await db.SaveChangesAsync();
            customerId = c.Id;
        }

        // tenant B nạp thẳng entity của A (bỏ qua filter bằng IgnoreQueryFilters) rồi thử sửa
        var ctxB = new TestTenantContext { TenantId = tenantB };
        using (var db = TestDb.Create(ctxB, dbName))
        {
            var stolen = await db.Customers.IgnoreQueryFilters()
                .SingleAsync(c => c.Id == customerId);
            stolen.FullName = "hacked-by-B";

            await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        }
    }
```

- [ ] **Step 2: Chạy test — kỳ vọng PASS (logic đã có từ Task 5)**

Run: `dotnet test --filter Cross_tenant_update_is_blocked`
Expected: Passed! 1 test.

- [ ] **Step 3: Chạy toàn bộ test tenancy**

Run: `dotnet test --filter Tenancy`
Expected: Passed! (toàn bộ read + write isolation tests).

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "test(infra): chặn sửa/xóa chéo tenant có test bảo vệ"
```

---

### Task 7: Wire DI trong API + endpoint /customers + integration test qua HTTP

**Files:**
- Create: `src/TourKit.Api/Tenancy/HttpTenantContext.cs`
- Modify: `src/TourKit.Api/Program.cs`
- Modify: `src/TourKit.Api/appsettings.json`
- Test: `tests/TourKit.Tests/Api/CustomerEndpointIsolationTests.cs`

- [ ] **Step 1: Viết `HttpTenantContext` (resolve tenant từ header — JWT sẽ thay ở Phase 0b)**

Create `src/TourKit.Api/Tenancy/HttpTenantContext.cs`:

```csharp
using TourKit.Shared.Tenancy;

namespace TourKit.Api.Tenancy;

/// <summary>
/// Tạm thời resolve tenant từ header "X-Tenant-Id".
/// Phase 0b sẽ thay bằng claim trong JWT (không tin client).
/// </summary>
public sealed class HttpTenantContext : ITenantContext
{
    public Guid TenantId { get; }
    public bool HasTenant => TenantId != Guid.Empty;

    public HttpTenantContext(IHttpContextAccessor accessor)
    {
        var raw = accessor.HttpContext?.Request.Headers["X-Tenant-Id"].ToString();
        TenantId = Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }
}
```

- [ ] **Step 2: Viết `Program.cs`**

Thay toàn bộ nội dung `src/TourKit.Api/Program.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using TourKit.Api.Tenancy;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Tenancy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ITenantContext, HttpTenantContext>();
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

var app = builder.Build();

app.MapGet("/customers", async (AppDbContext db) =>
    await db.Customers.Select(c => new { c.Id, c.FullName, c.Phone }).ToListAsync());

app.MapPost("/customers", async (AppDbContext db, CreateCustomer body) =>
{
    var c = new Customer { FullName = body.FullName, Phone = body.Phone };
    db.Customers.Add(c);
    await db.SaveChangesAsync();
    return Results.Created($"/customers/{c.Id}", new { c.Id });
});

app.Run();

record CreateCustomer(string FullName, string? Phone);

// Cho phép WebApplicationFactory trong test truy cập Program.
public partial class Program { }
```

- [ ] **Step 3: Cấu hình connection string LocalDB**

Thay `src/TourKit.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Server=(localdb)\\MSSQLLocalDB;Database=TourKit_Dev;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "Logging": { "LogLevel": { "Default": "Information", "Microsoft.AspNetCore": "Warning" } },
  "AllowedHosts": "*"
}
```

- [ ] **Step 4: Viết integration test cô lập qua HTTP**

Create `tests/TourKit.Tests/Api/CustomerEndpointIsolationTests.cs`:

```csharp
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TourKit.Infrastructure.Persistence;
using Xunit;

namespace TourKit.Tests.Api;

public class CustomerEndpointIsolationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CustomerEndpointIsolationTests(WebApplicationFactory<Program> factory)
    {
        // Thay SQL Server bằng InMemory để test không cần DB thật.
        _factory = factory.WithWebHostBuilder(b => b.ConfigureServices(services =>
        {
            var descriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            services.Remove(descriptor);
            services.AddDbContext<AppDbContext>(opt =>
                opt.UseInMemoryDatabase("EndpointIsolation"));
        }));
    }

    [Fact]
    public async Task Each_tenant_sees_only_its_own_customers_over_http()
    {
        var tenantA = Guid.NewGuid().ToString();
        var tenantB = Guid.NewGuid().ToString();
        var client = _factory.CreateClient();

        // tenant A tạo khách "A-http"
        var reqA = new HttpRequestMessage(HttpMethod.Post, "/customers");
        reqA.Headers.Add("X-Tenant-Id", tenantA);
        reqA.Content = JsonContent.Create(new { FullName = "A-http", Phone = (string?)null });
        (await client.SendAsync(reqA)).EnsureSuccessStatusCode();

        // tenant B đọc — không được thấy khách của A
        var reqB = new HttpRequestMessage(HttpMethod.Get, "/customers");
        reqB.Headers.Add("X-Tenant-Id", tenantB);
        var resB = await client.SendAsync(reqB);
        var listB = await resB.Content.ReadFromJsonAsync<List<CustomerDto>>();

        Assert.NotNull(listB);
        Assert.Empty(listB!);
    }

    private record CustomerDto(Guid Id, string FullName, string? Phone);
}
```

- [ ] **Step 5: Chạy test — kỳ vọng PASS**

Run: `dotnet test --filter Each_tenant_sees_only_its_own_customers_over_http`
Expected: Passed! 1 test.

- [ ] **Step 6: Tạo EF migration đầu tiên & apply lên LocalDB (kiểm chứng SQL Server thật)**

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate --project src/TourKit.Infrastructure --startup-project src/TourKit.Api
dotnet ef database update --project src/TourKit.Infrastructure --startup-project src/TourKit.Api
```

Expected: migration tạo bảng `Tenants`, `Customers` trên `TourKit_Dev`. Lệnh `database update` in `Done.`

- [ ] **Step 7: Chạy toàn bộ test suite**

Run: `dotnet test`
Expected: Passed! Tất cả test (read isolation, write isolation, cross-tenant block, HTTP isolation).

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "feat(api): DI tenancy + endpoint /customers + integration test cô lập HTTP + migration đầu tiên"
```

---

## Self-Review

**Spec coverage (mục 5 & 7 của spec):**
- Global Query Filter cô lập đọc → Task 4 ✅
- Auto-set TenantId khi ghi → Task 5 ✅
- Chặn thao tác chéo tenant + index theo tenant → Task 5/6 ✅ (index sẽ thêm ở migration của Phase 0b khi có nhiều entity; hiện đủ cho foundation)
- `ITenantContext` resolve (tạm header, JWT sau) → Task 7 ✅ + ghi chú Phase 0b
- Bảng hệ thống không có tenant (`Tenant`) → Task 3 ✅
- Timestamps/soft-delete trên BaseEntity → Task 2/5 ✅ (auto-filter soft-delete để lại; chưa cần cho isolation)

**Ngoài phạm vi (đúng chủ đích, sang Phase 0b):** JWT auth, RBAC/permission, đăng ký & provisioning tenant, subscription/plan, React shell. `Customer` là entity mẫu để chứng minh cô lập; sẽ chuyển sang module `Booking` khi tạo module đó.

**Placeholder scan:** Không có TBD/TODO; mọi step có code hoặc lệnh cụ thể + output kỳ vọng. ✅

**Type consistency:** `ITenantContext.TenantId`/`HasTenant`, `ITenantEntity.TenantId`, `BaseEntity.{Id,CreatedAt,UpdatedAt,IsDeleted}`, `AppDbContext(DbContextOptions, ITenantContext)` dùng nhất quán qua Task 2→7. ✅
