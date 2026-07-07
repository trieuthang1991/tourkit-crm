# Phase 1 — Catalog: Tour (TPT) + Template CRUD — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **BẮT BUỘC ĐỌC TRƯỚC:** `docs/conventions/backend-conventions.md` (§4 tenancy, §5 EF, §6 API) và `docs/business/database-optimization-analysis.md` §B3/§F/§G (thiết kế Tour TPT + lý do giữ 4 cột giá). Enforcement `.editorconfig`+`Directory.Build.props` ép net9, nullable, warnings-as-errors, braces, file-scoped ns, sealed record.

**Goal:** Người dùng đã đăng nhập (JWT, Phase 0b-1) quản lý **mẫu tour** (`TourTemplate`) của tenant mình: tạo/liệt kê/xem/sửa/xóa mềm, kèm lịch trình ngày (`TourItinerary`); dữ liệu cô lập tuyệt đối theo tenant. Nền TPT (`Tour` gốc + `TourTemplate`/`TourDeparture`) được dựng sẵn cho các phase sau (Booking dùng `TourDeparture`).

**Architecture:** `Tour` là entity **abstract** (BaseEntity + ITenantEntity) chứa cột chung; `TourTemplate`/`TourDeparture` kế thừa, map **TPT** (mỗi type 1 bảng, chia sẻ PK) qua EF Core `UseTptMappingStrategy()` + `ToTable()`. Vì TPT, global query filter chỉ áp ở **type gốc** (`BaseType is null`) — phải sửa vòng lặp trong `AppDbContext.OnModelCreating`. Phase 1 chỉ mở **endpoint cho TourTemplate** (catalog bán); `TourDeparture` có entity nhưng chưa có endpoint (dành Booking). Giá 4 nhóm tuổi giữ cột trực tiếp trên `TourTemplate` (denormalize có chủ đích — DB §A7).

**Tech Stack:** .NET 9, EF Core 9 (SQLite dev, TPT), ASP.NET Core Minimal API + JWT (đã có), xUnit + Mvc.Testing. Test dùng `AuthTestFactory` (đã có ở 0b-1) để seed tenant/user + login lấy JWT.

**Phạm vi Phase 1 (plan này) vs sau:** Plan này = Tour TPT + TourItinerary + **CRUD TourTemplate**. Defer sang **Phase 1b**: `PriceScenario` (giá theo cỡ đoàn), `TourAssignee` (người phụ trách), `MarketType` (thị trường), endpoint `TourDeparture` (mở chuyến — thuộc Booking).

---

## File Structure

```
src/TourKit.Infrastructure/
  Entities/Tour.cs                     # NEW — abstract base (Kind, Code, Title, dates, slots, status...)
  Entities/TourKind.cs                 # NEW — enum { Template, Departure }
  Entities/TourTemplate.cs             # NEW — : Tour, 4 cột giá + hoa hồng + ReservationHours + TermsNote
  Entities/TourDeparture.cs            # NEW — : Tour, AmountAdults/Children, AssignedToUserId, IsClosed...
  Entities/TourItinerary.cs            # NEW — ITenantEntity: TourId, DayIndex, Title, Detail
  Persistence/AppDbContext.cs          # MODIFY — DbSet<Tour/TourTemplate/TourDeparture/TourItinerary> + fix filter loop cho TPT
  Persistence/Configurations/TourConfiguration.cs          # NEW — TPT mapping + index chung
  Persistence/Configurations/TourTemplateConfiguration.cs  # NEW — ToTable + maxlength + decimal(18,2)
  Persistence/Configurations/TourDepartureConfiguration.cs # NEW — ToTable
  Persistence/Configurations/TourItineraryConfiguration.cs # NEW — index (TenantId, TourId, DayIndex)
  Migrations/*_AddTourCatalog.cs       # NEW (dotnet ef)
src/TourKit.Api/
  Catalog/TourTemplateContracts.cs     # NEW — Create/Update request + Response + Itinerary DTO
  Catalog/TourTemplateEndpoints.cs     # NEW — /api/v1/tour-templates CRUD + itinerary sub-resource
  Program.cs                           # MODIFY — app.MapTourTemplateEndpoints()
tests/TourKit.Tests/
  Catalog/TourTptPersistenceTests.cs   # NEW — TPT round-trip + tenant isolation ở tầng DbContext
  Catalog/TourTemplateEndpointTests.cs # NEW — CRUD + isolation qua JWT
```

**Nguyên tắc quyết định:** Tour ở `Infrastructure/Entities` (chưa tách module `Catalog` riêng — YAGNI ở Phase 1; sẽ tách project khi nhiều module). Endpoint ở `Api/Catalog`. TPT theo DB §B3. Chỉ Template có endpoint; Departure entity dựng sẵn để Booking dùng.

---

### Task 1: Tour TPT entities + fix filter loop + config + migration

**Files:**
- Create: `Entities/TourKind.cs`, `Tour.cs`, `TourTemplate.cs`, `TourDeparture.cs`, `TourItinerary.cs`
- Create: 4 file config trong `Persistence/Configurations/`
- Modify: `Persistence/AppDbContext.cs`

- [ ] **Step 1: enum `TourKind`**

Create `src/TourKit.Infrastructure/Entities/TourKind.cs`:

```csharp
namespace TourKit.Infrastructure.Entities;

public enum TourKind
{
    Template = 1,   // mẫu tour (catalog bán lại nhiều lần)
    Departure = 2,  // chuyến khởi hành cụ thể (điều hành/booking)
}
```

- [ ] **Step 2: abstract base `Tour`**

Create `src/TourKit.Infrastructure/Entities/Tour.cs`:

```csharp
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Entities;

/// <summary>Bảng gốc TPT — cột chung cho cả mẫu (Template) và chuyến (Departure).</summary>
public abstract class Tour : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public TourKind Kind { get; protected set; }
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? TourType { get; set; }              // inbound/outbound/domestic...
    public DateTimeOffset? DepartureDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public int TotalSlots { get; set; }
    public string? PickupPlace { get; set; }
    public string? DropoffPlace { get; set; }
    public string? TransportMode { get; set; }
    public Guid? ParentTourId { get; set; }            // Departure trỏ về Template nguồn (dùng ở Booking)
    public int Status { get; set; }
}
```

- [ ] **Step 3: `TourTemplate` (giá 4 nhóm tuổi giữ cột — DB §A7)**

Create `src/TourKit.Infrastructure/Entities/TourTemplate.cs`:

```csharp
namespace TourKit.Infrastructure.Entities;

public sealed class TourTemplate : Tour
{
    public TourTemplate() => Kind = TourKind.Template;

    public int ReservationHours { get; set; }          // thời hạn giữ chỗ (giờ)
    public decimal PriceAdult { get; set; }
    public decimal PriceChild { get; set; }
    public decimal PriceChildSmall { get; set; }
    public decimal PriceBaby { get; set; }
    public string? TermsNote { get; set; }
    public string? TermsNoteEn { get; set; }
}
```

- [ ] **Step 4: `TourDeparture` (entity dựng sẵn, chưa có endpoint)**

Create `src/TourKit.Infrastructure/Entities/TourDeparture.cs`:

```csharp
namespace TourKit.Infrastructure.Entities;

public sealed class TourDeparture : Tour
{
    public TourDeparture() => Kind = TourKind.Departure;

    public int AmountAdults { get; set; }
    public int AmountChildren { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public bool IsClosed { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
}
```

- [ ] **Step 5: `TourItinerary`**

Create `src/TourKit.Infrastructure/Entities/TourItinerary.cs`:

```csharp
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Entities;

public sealed class TourItinerary : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid TourId { get; set; }                   // trỏ tới Tour (Template hoặc Departure)
    public int DayIndex { get; set; }                  // ngày thứ mấy
    public string Title { get; set; } = string.Empty;
    public string? Detail { get; set; }
}
```

- [ ] **Step 6: Config Tour (TPT root) + các bảng phụ**

Create `src/TourKit.Infrastructure/Persistence/Configurations/TourConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Infrastructure.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class TourConfiguration : IEntityTypeConfiguration<Tour>
{
    public void Configure(EntityTypeBuilder<Tour> builder)
    {
        builder.UseTptMappingStrategy();               // TPT: mỗi type 1 bảng, chia sẻ PK
        builder.ToTable("Tours");

        builder.Property(x => x.Code).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(300);
        builder.Property(x => x.TourType).HasMaxLength(50);
        builder.Property(x => x.PickupPlace).HasMaxLength(300);
        builder.Property(x => x.DropoffPlace).HasMaxLength(300);
        builder.Property(x => x.TransportMode).HasMaxLength(100);

        // Index bắt đầu bằng TenantId (conventions §5 / DB §H).
        builder.HasIndex(x => new { x.TenantId, x.Kind, x.Status });
        builder.HasIndex(x => new { x.TenantId, x.Code });
        builder.HasIndex(x => new { x.TenantId, x.DepartureDate });
    }
}
```

Create `src/TourKit.Infrastructure/Persistence/Configurations/TourTemplateConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Infrastructure.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class TourTemplateConfiguration : IEntityTypeConfiguration<TourTemplate>
{
    public void Configure(EntityTypeBuilder<TourTemplate> builder)
    {
        builder.ToTable("TourTemplateFields");
        builder.Property(x => x.PriceAdult).HasPrecision(18, 2);
        builder.Property(x => x.PriceChild).HasPrecision(18, 2);
        builder.Property(x => x.PriceChildSmall).HasPrecision(18, 2);
        builder.Property(x => x.PriceBaby).HasPrecision(18, 2);
        builder.Property(x => x.TermsNote).HasMaxLength(4000);
        builder.Property(x => x.TermsNoteEn).HasMaxLength(4000);
    }
}
```

Create `src/TourKit.Infrastructure/Persistence/Configurations/TourDepartureConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Infrastructure.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class TourDepartureConfiguration : IEntityTypeConfiguration<TourDeparture>
{
    public void Configure(EntityTypeBuilder<TourDeparture> builder)
    {
        builder.ToTable("TourDepartureFields");
        builder.HasIndex(x => new { x.TenantId, x.IsClosed });
    }
}
```

Create `src/TourKit.Infrastructure/Persistence/Configurations/TourItineraryConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Infrastructure.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class TourItineraryConfiguration : IEntityTypeConfiguration<TourItinerary>
{
    public void Configure(EntityTypeBuilder<TourItinerary> builder)
    {
        builder.Property(x => x.Title).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Detail).HasMaxLength(4000);
        builder.HasIndex(x => new { x.TenantId, x.TourId, x.DayIndex });
    }
}
```

- [ ] **Step 7: Thêm DbSet + FIX vòng lặp filter cho TPT trong `AppDbContext`**

Trong `src/TourKit.Infrastructure/Persistence/AppDbContext.cs`:

(a) Thêm DbSet dưới `RefreshTokens`:

```csharp
    public DbSet<Tour> Tours => Set<Tour>();
    public DbSet<TourTemplate> TourTemplates => Set<TourTemplate>();
    public DbSet<TourDeparture> TourDepartures => Set<TourDeparture>();
    public DbSet<TourItinerary> TourItineraries => Set<TourItinerary>();
```

(b) Trong `OnModelCreating`, sửa vòng lặp áp query filter để **chỉ áp cho type gốc** (TPT: EF Core cấm HasQueryFilter trên type dẫn xuất). Thay đúng khối `foreach`:

```csharp
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // TPT: filter chỉ đặt ở type GỐC (BaseType == null); type dẫn xuất kế thừa filter của gốc.
            if (entityType.BaseType is not null)
            {
                continue;
            }

            var clr = entityType.ClrType;

            if (typeof(ITenantEntity).IsAssignableFrom(clr))
            {
                var filter = TenantFilterMethod.MakeGenericMethod(clr).Invoke(this, null);
                modelBuilder.Entity(clr).HasQueryFilter((LambdaExpression)filter!);
            }
            else if (typeof(BaseEntity).IsAssignableFrom(clr))
            {
                var filter = SoftDeleteFilterMethod.MakeGenericMethod(clr).Invoke(this, null);
                modelBuilder.Entity(clr).HasQueryFilter((LambdaExpression)filter!);
            }
        }
```

Lưu ý: `Tour` (abstract, gốc, `ITenantEntity`) sẽ nhận tenant filter; `TourTemplate`/`TourDeparture` (dẫn xuất) bị `continue` bỏ qua nhưng kế thừa filter của `Tour`. `BuildTenantFilter<Tour>` cần `Tour : BaseEntity, ITenantEntity` — thỏa vì `Tour` implement cả hai.

- [ ] **Step 8: Build + migration**

```bash
dotnet build
export PATH="$PATH:$HOME/.dotnet/tools"
dotnet ef migrations add AddTourCatalog --project src/TourKit.Infrastructure --startup-project src/TourKit.Api
dotnet ef database update --project src/TourKit.Infrastructure --startup-project src/TourKit.Api
```
Expected: tạo bảng `Tours`, `TourTemplateFields`, `TourDepartureFields`, `TourItineraries`; `TourTemplateFields`/`TourDepartureFields` chia sẻ PK với `Tours` (TPT). Build 0 Warning/0 Error. `database update` in `Done.`

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "feat(catalog): Tour TPT (Template/Departure) + Itinerary + fix filter loop cho TPT + migration"
```

---

### Task 2: Test tầng DbContext — TPT round-trip + cô lập tenant

**Files:**
- Create: `tests/TourKit.Tests/Catalog/TourTptPersistenceTests.cs`

- [ ] **Step 1: Viết test (FAIL trước — chưa chắc, nhưng chạy để xác nhận hành vi TPT + filter)**

Create `tests/TourKit.Tests/Catalog/TourTptPersistenceTests.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Entities;
using TourKit.Tests.Support;

namespace TourKit.Tests.Catalog;

public class TourTptPersistenceTests
{
    [Fact]
    public async Task Template_saved_and_queried_via_OfType()
    {
        var tenant = new TestTenantContext { TenantId = Guid.NewGuid() };
        var db = TestDb.Create(tenant, nameof(Template_saved_and_queried_via_OfType));

        db.TourTemplates.Add(new TourTemplate
        {
            Code = "T-001", Title = "Đà Nẵng 3N2Đ", TotalSlots = 30, PriceAdult = 5_000_000m,
        });
        await db.SaveChangesAsync();

        var templates = await db.Tours.OfType<TourTemplate>().ToListAsync();
        Assert.Single(templates);
        Assert.Equal(TourKind.Template, templates[0].Kind);
        Assert.Equal(5_000_000m, templates[0].PriceAdult);
    }

    [Fact]
    public async Task Templates_are_isolated_per_tenant()
    {
        var dbName = nameof(Templates_are_isolated_per_tenant);
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        using (var db = TestDb.Create(new TestTenantContext { TenantId = tenantA }, dbName))
        {
            db.TourTemplates.Add(new TourTemplate { Code = "A-1", Title = "A" });
            await db.SaveChangesAsync();
        }

        using (var db = TestDb.Create(new TestTenantContext { TenantId = tenantB }, dbName))
        {
            db.TourTemplates.Add(new TourTemplate { Code = "B-1", Title = "B" });
            await db.SaveChangesAsync();

            var titles = await db.TourTemplates.Select(t => t.Title).ToListAsync();
            Assert.Equal(new[] { "B" }, titles);
        }
    }
}
```

- [ ] **Step 2: Chạy test — kỳ vọng PASS**

Run: `dotnet test --filter TourTptPersistenceTests`
Expected: Passed! 2 tests. (Nếu FAIL do EF cấm filter trên type dẫn xuất → xem lại fix Task 1 Step 7b.)

Note InMemory + TPT: InMemory provider bỏ qua chiến lược bảng nên vẫn chạy; test tầng SQLite thật đã có ở migration Task 1.

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "test(catalog): TPT round-trip + cô lập tenant cho TourTemplate"
```

---

### Task 3: TourTemplate contracts + endpoints tạo/liệt kê/xem

**Files:**
- Create: `src/TourKit.Api/Catalog/TourTemplateContracts.cs`, `Catalog/TourTemplateEndpoints.cs`
- Modify: `src/TourKit.Api/Program.cs`
- Test: `tests/TourKit.Tests/Catalog/TourTemplateEndpointTests.cs`

- [ ] **Step 1: DTO**

Create `src/TourKit.Api/Catalog/TourTemplateContracts.cs`:

```csharp
namespace TourKit.Api.Catalog;

public sealed record CreateTourTemplateRequest(
    string Code, string Title, string? TourType, int TotalSlots, int ReservationHours,
    decimal PriceAdult, decimal PriceChild, decimal PriceChildSmall, decimal PriceBaby,
    string? TermsNote);

public sealed record UpdateTourTemplateRequest(
    string Title, string? TourType, int TotalSlots, int ReservationHours,
    decimal PriceAdult, decimal PriceChild, decimal PriceChildSmall, decimal PriceBaby,
    string? TermsNote);

public sealed record TourTemplateResponse(
    Guid Id, string Code, string Title, string? TourType, int TotalSlots, int ReservationHours,
    decimal PriceAdult, decimal PriceChild, decimal PriceChildSmall, decimal PriceBaby,
    string? TermsNote, int Status);
```

- [ ] **Step 2: Endpoints (create/list/get)**

Create `src/TourKit.Api/Catalog/TourTemplateEndpoints.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using TourKit.Infrastructure.Entities;
using TourKit.Infrastructure.Persistence;

namespace TourKit.Api.Catalog;

public static class TourTemplateEndpoints
{
    public static IEndpointRouteBuilder MapTourTemplateEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tour-templates").RequireAuthorization();

        group.MapGet("/", async (AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.TourTemplates.AsNoTracking()
                .OrderBy(t => t.Title)
                .Select(t => ToResponse(t)).ToListAsync(ct)));

        group.MapGet("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var t = await db.TourTemplates.AsNoTracking()
                .Where(x => x.Id == id).Select(x => ToResponse(x)).FirstOrDefaultAsync(ct);
            return t is null ? Results.NotFound() : Results.Ok(t);
        });

        group.MapPost("/", async (CreateTourTemplateRequest body, AppDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.Code) || string.IsNullOrWhiteSpace(body.Title))
            {
                return Validation("Code và Title là bắt buộc.");
            }

            var t = new TourTemplate
            {
                Code = body.Code.Trim(), Title = body.Title.Trim(), TourType = body.TourType,
                TotalSlots = body.TotalSlots, ReservationHours = body.ReservationHours,
                PriceAdult = body.PriceAdult, PriceChild = body.PriceChild,
                PriceChildSmall = body.PriceChildSmall, PriceBaby = body.PriceBaby,
                TermsNote = body.TermsNote,
            };
            db.TourTemplates.Add(t);
            await db.SaveChangesAsync(ct);
            return Results.Created($"/api/v1/tour-templates/{t.Id}", ToResponse(t));
        });

        return app;
    }

    // Projection dùng chung — LINQ dịch được vì chỉ gán thuộc tính.
    private static TourTemplateResponse ToResponse(TourTemplate t) => new(
        t.Id, t.Code, t.Title, t.TourType, t.TotalSlots, t.ReservationHours,
        t.PriceAdult, t.PriceChild, t.PriceChildSmall, t.PriceBaby, t.TermsNote, t.Status);

    private static IResult Validation(string message) =>
        Results.ValidationProblem(new Dictionary<string, string[]> { ["Request"] = [message] });
}
```

- [ ] **Step 3: Wire vào `Program.cs`**

Thêm `using TourKit.Api.Catalog;` và sau `app.MapCustomerEndpoints();` thêm:

```csharp
app.MapTourTemplateEndpoints();
```

- [ ] **Step 4: Test integration (create + list + get + isolation)**

Create `tests/TourKit.Tests/Catalog/TourTemplateEndpointTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Api.Catalog;
using TourKit.Tests.Support;

namespace TourKit.Tests.Catalog;

public class TourTemplateEndpointTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public TourTemplateEndpointTests(AuthTestFactory factory) => _factory = factory;

    private async Task<HttpClient> LoggedInClientAsync(string slug)
    {
        var (s, email, password) = await _factory.SeedTenantUserAsync(slug);
        var client = _factory.CreateClient();
        var auth = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(s, email, password))).Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    private static CreateTourTemplateRequest Sample(string code) =>
        new(code, "Đà Nẵng 3N2Đ", "domestic", 30, 24, 5_000_000m, 3_000_000m, 2_000_000m, 0m, "Điều khoản");

    [Fact]
    public async Task Create_then_list_and_get()
    {
        var client = await LoggedInClientAsync("cat-a");

        var created = await client.PostAsJsonAsync("/api/v1/tour-templates", Sample("T-001"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var dto = await created.Content.ReadFromJsonAsync<TourTemplateResponse>();
        Assert.NotNull(dto);
        Assert.Equal(5_000_000m, dto!.PriceAdult);

        var list = await client.GetFromJsonAsync<List<TourTemplateResponse>>("/api/v1/tour-templates");
        Assert.Single(list!);

        var got = await client.GetFromJsonAsync<TourTemplateResponse>($"/api/v1/tour-templates/{dto.Id}");
        Assert.Equal("T-001", got!.Code);
    }

    [Fact]
    public async Task Requires_auth()
    {
        var client = _factory.CreateClient();   // không login
        var res = await client.GetAsync("/api/v1/tour-templates");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Isolated_between_tenants()
    {
        var clientA = await LoggedInClientAsync("cat-iso-a");
        await clientA.PostAsJsonAsync("/api/v1/tour-templates", Sample("A-1"));

        var clientB = await LoggedInClientAsync("cat-iso-b");
        var listB = await clientB.GetFromJsonAsync<List<TourTemplateResponse>>("/api/v1/tour-templates");
        Assert.Empty(listB!);
    }
}
```

- [ ] **Step 5: Build + test**

Run: `dotnet test --filter TourTemplateEndpointTests`
Expected: Passed! 3 tests. Build 0 Warning/0 Error.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat(catalog): endpoint tạo/liệt kê/xem TourTemplate + test cô lập qua JWT"
```

---

### Task 4: TourTemplate cập nhật + xóa mềm

**Files:**
- Modify: `src/TourKit.Api/Catalog/TourTemplateEndpoints.cs`
- Test: `tests/TourKit.Tests/Catalog/TourTemplateEndpointTests.cs`

- [ ] **Step 1: Thêm PUT + DELETE vào `MapTourTemplateEndpoints`** (trước `return app;`)

```csharp
        group.MapPut("/{id:guid}", async (Guid id, UpdateTourTemplateRequest body, AppDbContext db, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(body.Title))
            {
                return Validation("Title là bắt buộc.");
            }

            var t = await db.TourTemplates.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (t is null)
            {
                return Results.NotFound();
            }

            t.Title = body.Title.Trim();
            t.TourType = body.TourType;
            t.TotalSlots = body.TotalSlots;
            t.ReservationHours = body.ReservationHours;
            t.PriceAdult = body.PriceAdult;
            t.PriceChild = body.PriceChild;
            t.PriceChildSmall = body.PriceChildSmall;
            t.PriceBaby = body.PriceBaby;
            t.TermsNote = body.TermsNote;
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });

        group.MapDelete("/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var t = await db.TourTemplates.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (t is null)
            {
                return Results.NotFound();
            }

            t.IsDeleted = true;   // soft delete (conventions §5)
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
```

- [ ] **Step 2: Thêm test update + delete**

Thêm vào `TourTemplateEndpointTests`:

```csharp
    [Fact]
    public async Task Update_then_soft_delete()
    {
        var client = await LoggedInClientAsync("cat-upd");
        var dto = await (await client.PostAsJsonAsync("/api/v1/tour-templates", Sample("U-1")))
            .Content.ReadFromJsonAsync<TourTemplateResponse>();

        var upd = await client.PutAsJsonAsync($"/api/v1/tour-templates/{dto!.Id}",
            new UpdateTourTemplateRequest("Huế 2N1Đ", "domestic", 20, 12, 4_000_000m, 2_500_000m, 1_500_000m, 0m, "ĐK mới"));
        Assert.Equal(HttpStatusCode.NoContent, upd.StatusCode);

        var got = await client.GetFromJsonAsync<TourTemplateResponse>($"/api/v1/tour-templates/{dto.Id}");
        Assert.Equal("Huế 2N1Đ", got!.Title);

        var del = await client.DeleteAsync($"/api/v1/tour-templates/{dto.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var list = await client.GetFromJsonAsync<List<TourTemplateResponse>>("/api/v1/tour-templates");
        Assert.Empty(list!);   // soft-deleted bị filter ẩn
    }
```

- [ ] **Step 3: Test**

Run: `dotnet test --filter TourTemplateEndpointTests`
Expected: Passed! 4 tests.

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "feat(catalog): cập nhật + xóa mềm TourTemplate + test"
```

---

### Task 5: Lịch trình ngày (TourItinerary) cho template

**Files:**
- Modify: `src/TourKit.Api/Catalog/TourTemplateContracts.cs`, `Catalog/TourTemplateEndpoints.cs`
- Test: `tests/TourKit.Tests/Catalog/TourTemplateEndpointTests.cs`

- [ ] **Step 1: DTO itinerary** — thêm vào `TourTemplateContracts.cs`:

```csharp
public sealed record ItineraryDayRequest(int DayIndex, string Title, string? Detail);
public sealed record ItineraryDayResponse(Guid Id, int DayIndex, string Title, string? Detail);
```

- [ ] **Step 2: Sub-resource endpoints** — thêm vào `MapTourTemplateEndpoints` (trước `return app;`). Thay toàn bộ lịch trình của template bằng danh sách gửi lên (đơn giản, idempotent):

```csharp
        group.MapGet("/{id:guid}/itinerary", async (Guid id, AppDbContext db, CancellationToken ct) =>
            Results.Ok(await db.TourItineraries.AsNoTracking()
                .Where(i => i.TourId == id).OrderBy(i => i.DayIndex)
                .Select(i => new ItineraryDayResponse(i.Id, i.DayIndex, i.Title, i.Detail))
                .ToListAsync(ct)));

        group.MapPut("/{id:guid}/itinerary", async (Guid id, ItineraryDayRequest[] body, AppDbContext db, CancellationToken ct) =>
        {
            var exists = await db.TourTemplates.AnyAsync(x => x.Id == id, ct);
            if (!exists)
            {
                return Results.NotFound();
            }

            var old = await db.TourItineraries.Where(i => i.TourId == id).ToListAsync(ct);
            db.TourItineraries.RemoveRange(old);   // hard-remove dòng lịch cũ rồi ghi lại (thay toàn bộ)
            foreach (var day in body)
            {
                db.TourItineraries.Add(new TourItinerary
                {
                    TourId = id, DayIndex = day.DayIndex, Title = day.Title.Trim(), Detail = day.Detail,
                });
            }

            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        });
```

Lưu ý: `RemoveRange` xóa cứng dòng lịch trình (không phải dữ liệu nghiệp vụ giao dịch — chấp nhận). Interceptor chặn chéo tenant tự bảo vệ (dòng cũ đã bị query filter lọc theo tenant nên chỉ xóa dòng của tenant hiện tại).

- [ ] **Step 3: Test itinerary**

Thêm vào `TourTemplateEndpointTests`:

```csharp
    [Fact]
    public async Task Set_and_get_itinerary()
    {
        var client = await LoggedInClientAsync("cat-itin");
        var dto = await (await client.PostAsJsonAsync("/api/v1/tour-templates", Sample("I-1")))
            .Content.ReadFromJsonAsync<TourTemplateResponse>();

        var days = new[]
        {
            new ItineraryDayRequest(1, "Ngày 1: Khởi hành", "Bay Hà Nội - Đà Nẵng"),
            new ItineraryDayRequest(2, "Ngày 2: Tham quan", "Bà Nà Hills"),
        };
        var put = await client.PutAsJsonAsync($"/api/v1/tour-templates/{dto!.Id}/itinerary", days);
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var got = await client.GetFromJsonAsync<List<ItineraryDayResponse>>($"/api/v1/tour-templates/{dto.Id}/itinerary");
        Assert.Equal(2, got!.Count);
        Assert.Equal("Ngày 1: Khởi hành", got[0].Title);
    }
```

- [ ] **Step 4: Test**

Run: `dotnet test --filter TourTemplateEndpointTests`
Expected: Passed! 5 tests.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "feat(catalog): lịch trình ngày (TourItinerary) cho template + test"
```

---

### Task 6: Chạy toàn bộ suite + kiểm chứng migration + hoàn tất

**Files:** (không tạo file mới)

- [ ] **Step 1: Toàn bộ test**

Run: `dotnet test`
Expected: Passed! Tất cả (Phase 0a tenancy + 0b-1 auth + Phase 1 catalog). 0 failed, 0 skipped.

- [ ] **Step 2: Kiểm chứng migration trên SQLite thật** (nếu chưa chạy ở Task 1)

```bash
export PATH="$PATH:$HOME/.dotnet/tools"
dotnet ef database update --project src/TourKit.Infrastructure --startup-project src/TourKit.Api
```
Expected: `Done.` — bảng `Tours`/`TourTemplateFields`/`TourDepartureFields`/`TourItineraries` tồn tại.

- [ ] **Step 3: Commit (nếu có thay đổi)** — thường không, đã commit theo task.

---

## Self-Review

**Spec coverage:**
- Tour TPT (base + Template + Departure) → Task 1 ✅ (DB §B3)
- Fix query filter cho TPT (chỉ type gốc) → Task 1 Step 7b ✅ (rủi ro lớn nhất, đã xử lý)
- Giá 4 nhóm tuổi giữ cột → Task 1 Step 3 ✅ (DB §A7)
- Index TenantId-first → Task 1 config ✅ (DB §H)
- CRUD TourTemplate + itinerary, JWT-protected, tenant-isolated → Task 3/4/5 ✅
- Test cô lập tenant (DbContext + HTTP) → Task 2/3 ✅

**Ngoài phạm vi (đúng chủ đích, Phase 1b / Booking):** `PriceScenario`, `TourAssignee`, `MarketType`, endpoint `TourDeparture` (mở chuyến), FluentValidation đầy đủ (hiện validation tối thiểu như 0a/0b), RBAC (endpoint mới `RequireAuthorization` nhưng chưa phân quyền theo role — chờ 0b-2).

**Placeholder scan:** Không TBD/TODO; mọi step có code + lệnh + output kỳ vọng. ✅

**Type consistency:** `TourKind{Template,Departure}`, `Tour.Kind` (protected set, gán trong ctor dẫn xuất), `TourTemplate.{PriceAdult..PriceBaby,ReservationHours,TermsNote}`, `CreateTourTemplateRequest`/`UpdateTourTemplateRequest`/`TourTemplateResponse`, `ItineraryDayRequest/Response`, endpoint `MapTourTemplateEndpoints`, DbSet `TourTemplates/TourItineraries` — nhất quán Task 1→5. ✅

**Rủi ro đã lường:**
- EF Core cấm `HasQueryFilter` trên type dẫn xuất TPT → fix vòng lặp chỉ áp cho `BaseType is null` (Task 1 Step 7b). Nếu Task 2 test lỗi "derived type" → kiểm lại chỗ này.
- `Tour.Kind` set trong constructor của `TourTemplate`/`TourDeparture` (không cho client đổi) → `protected set`.
- InMemory provider bỏ qua TPT table strategy (test tầng DbContext vẫn chạy); TPT thật được kiểm bằng migration SQLite (Task 1 Step 8).
