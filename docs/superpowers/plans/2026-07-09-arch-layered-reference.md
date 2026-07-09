# Chuyển kiến trúc sang Phân tầng (N-tier) — Module mẫu Customers

> REQUIRED SUB-SKILL: superpowers:executing-plans / subagent-driven-development. Steps checkbox.

**Goal:** Chuyển từ Vertical Slice/CQRS sang **kiến trúc phân tầng cổ điển** (dễ tiếp cận), làm **Customers làm module mẫu** để chốt pattern trước khi nhân rộng. Vẫn là **monolith** (không microservices).

**Quyết định đã chốt:** Repository = `IRepository<T>` generic + repo riêng khi cần · Lỗi = Exception + global handler → ProblemDetails · Triển khai = module mẫu trước.

**Kiến trúc đích (chiều phụ thuộc hướng vào trong):**
```
TourKit.Api            Controllers [ApiController] + middleware + DI + auth
   │  refs Application + Infrastructure
TourKit.Application    Services (ICustomerService/CustomerService) + DTOs + IRepository<T> + Exceptions + Validators
   │  refs Domain (Shared)
TourKit.Infrastructure Repository<T> (EF) + AppDbContext + EF config/migration + multitenancy
   │  refs Application + Domain (Shared)
TourKit.Shared (Domain) Entities + BaseEntity + công thức (BookingMath/OrderMath) + ITenantContext
```
Luồng 1 request: `Controller → Service → IRepository<T>/Repository<T> → EF Core`. Service ném `NotFoundException`/`ConflictException`/`ValidationException`; middleware map sang HTTP.

**Giữ nguyên:** đa tenant (global filter + timestamp interceptor trong AppDbContext), JWT/RBAC (permission policy), config SQLite↔SqlServer, converter decimal/DateTimeOffset cho SQLite, migrate-on-startup.

**Lưu ý chuyển tiếp:** 10 module còn lại vẫn chạy CQRS trong lúc mẫu Customers được review. Kernel CQRS (`TourKit.Shared.Application.*` + Dispatcher) chỉ xoá khi nhân rộng xong.

---

## Task 1: Chuyển Entities + công thức xuống tầng Domain (Shared)

Để Application tham chiếu được entity mà không lệ thuộc Infrastructure, entity phải nằm ở tầng thấp nhất.

**Files:** ~46 entity trong `src/TourKit.Infrastructure/Entities/` → `src/TourKit.Shared/Entities/`; `src/TourKit.Infrastructure/Domain/{BookingMath,OrderMath,ReceiptQueries}.cs` → `src/TourKit.Shared/Domain/`. Update ~186 file dùng `using TourKit.Infrastructure.Entities;` và ~16 file dùng `TourKit.Infrastructure.Domain`.

- [ ] **Step 1: Di chuyển file (giữ nội dung, đổi namespace).**
  - `git mv src/TourKit.Infrastructure/Entities/*.cs src/TourKit.Shared/Entities/` — đổi namespace mỗi file `TourKit.Infrastructure.Entities` → `TourKit.Shared.Entities`.
  - `git mv src/TourKit.Infrastructure/Domain/*.cs src/TourKit.Shared/Domain/` — namespace `TourKit.Infrastructure.Domain` → `TourKit.Shared.Domain`.
  - `ReceiptQueries` dùng `IQueryable`/EF? Nếu chỉ dùng `System.Linq` (không `Microsoft.EntityFrameworkCore`) thì move được. Nếu dính EF (`.Where` trên `IQueryable` là System.Linq — OK) — kiểm tra: nếu file import `Microsoft.EntityFrameworkCore` thì GIỮ ở Infrastructure (arch-test cấm Shared dính EF). `BookingMath`/`OrderMath` thuần → move.
- [ ] **Step 2: Cập nhật using toàn repo** (sed, cơ học):
  - `grep -rl "TourKit.Infrastructure.Entities" src tests | xargs sed -i 's/TourKit\.Infrastructure\.Entities/TourKit.Shared.Entities/g'`
  - Tương tự cho `TourKit.Infrastructure.Domain` → `TourKit.Shared.Domain` (chỉ các file move được; nếu ReceiptQueries ở lại thì trừ nó ra).
  - AppDbContext + configs (Infrastructure) đã `using TourKit.Shared.Entities` cho BaseEntity — nay entity cùng namespace, có thể bỏ `using TourKit.Infrastructure.Entities` trùng. Để compiler báo `using` thừa (analyzer IDE0005) → xoá.
- [ ] **Step 3: Migrations** — file migration cũ có `using TourKit.Infrastructure.Entities`? Thường không (chúng dùng `migrationBuilder` thuần). Nếu snapshot/Designer tham chiếu entity type qua string thì không sao. Build để lộ lỗi.
- [ ] **Step 4: Build + test.** `dotnet build` 0/0 (sửa hết `using` sai/thừa); `dotnet test` full xanh (149). KHÔNG đổi hành vi.
- [ ] **Step 5: Arch test** — cập nhật `tests/TourKit.ArchTests/LayeringTests.cs`: Shared vẫn không dính EF (BookingMath/OrderMath thuần); thêm sau ở Task 5.
- [ ] **Step 6: Commit** `refactor(arch): chuyển Entities + công thức xuống tầng Domain (Shared)`.

---

## Task 2: Tạo project TourKit.Application (Services + DTO + Repository interface + Exceptions)

**Files:**
- `src/TourKit.Application/TourKit.Application.csproj` (net9.0, ref `TourKit.Shared`; bật analyzers giống project khác — copy `<PropertyGroup>` từ TourKit.Infrastructure.csproj: TreatWarningsAsErrors, Nullable, etc.)
- `src/TourKit.Application/Common/IRepository.cs`
- `src/TourKit.Application/Common/Exceptions.cs`
- `src/TourKit.Application/Common/PagedResult.cs`
- `src/TourKit.Application/Customers/{CustomerDtos.cs,ICustomerService.cs,CustomerService.cs,CustomerValidators.cs}`
- Thêm ref vào solution: `dotnet sln add src/TourKit.Application/TourKit.Application.csproj`; Api + Infrastructure ref Application (Task 3/4).

- [ ] **Step 1: `IRepository<T>`** (generic, đủ CRUD + query cơ bản):
```csharp
using System.Linq.Expressions;
using TourKit.Shared.Entities;

namespace TourKit.Application.Common;

/// <summary>Repository chung cho aggregate/entity. Query phức tạp → thêm interface repo riêng.</summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);
    Task<(IReadOnlyList<T> Items, int Total)> PageAsync(int page, int size, Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    void Update(T entity);
    void Remove(T entity);
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
}
```
- [ ] **Step 2: Exceptions** (`Common/Exceptions.cs`):
```csharp
namespace TourKit.Application.Common;

public abstract class AppException(string message) : Exception(message)
{
    public abstract string ErrorType { get; }   // "not_found" | "conflict" | "validation" | "forbidden"
}
public sealed class NotFoundException(string message = "Không tìm thấy dữ liệu.") : AppException(message)
{ public override string ErrorType => "not_found"; }
public sealed class ConflictException(string message) : AppException(message)
{ public override string ErrorType => "conflict"; }
public sealed class ValidationAppException(string message) : AppException(message)
{ public override string ErrorType => "validation"; }
public sealed class ForbiddenException(string message = "Không có quyền.") : AppException(message)
{ public override string ErrorType => "forbidden"; }
```
- [ ] **Step 3: `PagedResult<T>`** (`Common/PagedResult.cs`): `public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int Size);`
- [ ] **Step 4: Customer DTOs** (`Customers/CustomerDtos.cs`):
```csharp
namespace TourKit.Application.Customers;

public sealed record CustomerDto(Guid Id, string FullName, string? Phone);
public sealed record CreateCustomerDto(string FullName, string? Phone);
public sealed record UpdateCustomerDto(string FullName, string? Phone);
```
- [ ] **Step 5: `ICustomerService`**:
```csharp
using TourKit.Application.Common;

namespace TourKit.Application.Customers;

public interface ICustomerService
{
    Task<PagedResult<CustomerDto>> ListAsync(int page, int size, CancellationToken ct = default);
    Task<CustomerDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<CustomerDto> CreateAsync(CreateCustomerDto dto, CancellationToken ct = default);
    Task UpdateAsync(Guid id, UpdateCustomerDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
```
- [ ] **Step 6: Validators** (FluentValidation, `Customers/CustomerValidators.cs`): `CreateCustomerValidator : AbstractValidator<CreateCustomerDto>` RuleFor FullName NotEmpty; `UpdateCustomerValidator` tương tự. (Add package FluentValidation vào Application csproj.)
- [ ] **Step 7: `CustomerService`** (business logic; ném exception; validate; map entity↔DTO):
```csharp
using FluentValidation;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Customers;

public sealed class CustomerService(
    IRepository<Customer> repo,
    IValidator<CreateCustomerDto> createValidator,
    IValidator<UpdateCustomerDto> updateValidator) : ICustomerService
{
    public async Task<PagedResult<CustomerDto>> ListAsync(int page, int size, CancellationToken ct = default)
    {
        var (items, total) = await repo.PageAsync(page, size, ct: ct);
        return new PagedResult<CustomerDto>(items.Select(Map).ToList(), total, page, size);
    }

    public async Task<CustomerDto> GetAsync(Guid id, CancellationToken ct = default)
        => Map(await repo.GetByIdAsync(id, ct) ?? throw new NotFoundException());

    public async Task<CustomerDto> CreateAsync(CreateCustomerDto dto, CancellationToken ct = default)
    {
        await Validate(createValidator, dto, ct);
        var entity = new Customer { FullName = dto.FullName.Trim(), Phone = dto.Phone };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        return Map(entity);
    }

    public async Task UpdateAsync(Guid id, UpdateCustomerDto dto, CancellationToken ct = default)
    {
        await Validate(updateValidator, dto, ct);
        var entity = await repo.GetByIdAsync(id, ct) ?? throw new NotFoundException();
        entity.FullName = dto.FullName.Trim();
        entity.Phone = dto.Phone;
        repo.Update(entity);
        await repo.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await repo.GetByIdAsync(id, ct) ?? throw new NotFoundException();
        repo.Remove(entity);
        await repo.SaveChangesAsync(ct);
    }

    private static async Task Validate<T>(IValidator<T> v, T dto, CancellationToken ct)
    {
        var r = await v.ValidateAsync(dto, ct);
        if (!r.IsValid)
        {
            throw new ValidationAppException(r.Errors[0].ErrorMessage);
        }
    }

    private static CustomerDto Map(Customer c) => new(c.Id, c.FullName, c.Phone);
}
```
- [ ] **Step 8: Build + commit** `feat(arch): tầng Application — IRepository + Exceptions + CustomerService (module mẫu)`.

---

## Task 3: Infrastructure — Repository<T> generic + đăng ký

**Files:**
- `src/TourKit.Infrastructure/TourKit.Infrastructure.csproj` — thêm `<ProjectReference ..\TourKit.Application\TourKit.Application.csproj />`
- `src/TourKit.Infrastructure/Repositories/Repository.cs`

- [ ] **Step 1: `Repository<T>`** (dùng AppDbContext; global tenant filter đã tự áp):
```csharp
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Repositories;

public sealed class Repository<T>(AppDbContext db) : IRepository<T> where T : BaseEntity
{
    private DbSet<T> Set => db.Set<T>();

    public Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => Set.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
        => await (predicate is null ? Set : Set.Where(predicate)).AsNoTracking().ToListAsync(ct);

    public async Task<(IReadOnlyList<T> Items, int Total)> PageAsync(int page, int size, Expression<Func<T, bool>>? predicate = null, CancellationToken ct = default)
    {
        var q = predicate is null ? Set : Set.Where(predicate);
        var p = page < 1 ? 1 : page;
        var s = size is < 1 or > 200 ? 20 : size;
        var total = await q.CountAsync(ct);
        var items = await q.AsNoTracking().OrderByDescending(e => e.CreatedAt).Skip((p - 1) * s).Take(s).ToListAsync(ct);
        return (items, total);
    }

    public async Task AddAsync(T entity, CancellationToken ct = default) => await Set.AddAsync(entity, ct);
    public void Update(T entity) => Set.Update(entity);
    public void Remove(T entity) => Set.Remove(entity);
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default) => Set.AnyAsync(predicate, ct);
}
```
Ghi chú: `OrderByDescending(CreatedAt)` — CreatedAt là DateTimeOffset, SQLite dùng converter long (đã có) nên sort OK.
- [ ] **Step 2: Build + commit** `feat(arch): Repository<T> generic (EF Core) tầng Infrastructure`.

---

## Task 4: Api — CustomersController + global exception handler + DI

**Files:**
- `src/TourKit.Api/TourKit.Api.csproj` — thêm ref `TourKit.Application`.
- `src/TourKit.Api/Controllers/CustomersController.cs`
- `src/TourKit.Api/Middleware/ExceptionHandlingMiddleware.cs`
- Modify `src/TourKit.Api/Program.cs` — `AddControllers()`, đăng ký `IRepository<>`+services, `UseMiddleware<ExceptionHandlingMiddleware>()`, `MapControllers()`, BỎ `MapCustomerEndpoints()`.
- Xoá module CQRS Customers: `src/TourKit.Api/Customers/{CustomerEndpoints.cs,CustomerContracts.cs,Features/*}`.

- [ ] **Step 1: `ExceptionHandlingMiddleware`** — map `AppException.ErrorType` → HTTP + ProblemDetails (validation→400, not_found→404, conflict→409, forbidden→403; khác→500):
```csharp
using TourKit.Application.Common;

namespace TourKit.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try { await next(ctx); }
        catch (AppException ex)
        {
            var status = ex.ErrorType switch
            {
                "not_found" => StatusCodes.Status404NotFound,
                "conflict" => StatusCodes.Status409Conflict,
                "forbidden" => StatusCodes.Status403Forbidden,
                _ => StatusCodes.Status400BadRequest,
            };
            await Results.Problem(detail: ex.Message, statusCode: status, title: ex.ErrorType).ExecuteAsync(ctx);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi chưa xử lý");
            await Results.Problem(detail: "Đã có lỗi xảy ra.", statusCode: 500).ExecuteAsync(ctx);
        }
    }
}
```
- [ ] **Step 2: `CustomersController`** (mỏng, gọi service):
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Authz;
using TourKit.Application.Customers;

namespace TourKit.Api.Controllers;

[ApiController]
[Route("api/v1/customers")]
public sealed class CustomersController(ICustomerService service) : ControllerBase
{
    [HttpGet]
    [Authorize(Permissions.CustomerView)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int size = 20, CancellationToken ct = default)
        => Ok(await service.ListAsync(page, size, ct));

    [HttpGet("{id:guid}")]
    [Authorize(Permissions.CustomerView)]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct)
        => Ok(await service.GetAsync(id, ct));

    [HttpPost]
    [Authorize(Permissions.CustomerCreate)]
    public async Task<IActionResult> Create([FromBody] CreateCustomerDto dto, CancellationToken ct)
    {
        var created = await service.CreateAsync(dto, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Permissions.CustomerUpdate)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerDto dto, CancellationToken ct)
    {
        await service.UpdateAsync(id, dto, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Permissions.CustomerDelete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return NoContent();
    }
}
```
- [ ] **Step 3: Program.cs DI** — thêm:
  - `builder.Services.AddControllers();`
  - `builder.Services.AddScoped(typeof(IRepository<>), typeof(TourKit.Infrastructure.Repositories.Repository<>));`
  - Đăng ký services (tạm thời tường minh cho mẫu): `builder.Services.AddScoped<ICustomerService, CustomerService>();` — khi nhân rộng sẽ scan tự động.
  - FluentValidation validators đã `AddValidatorsFromAssemblyContaining<Program>()` — mở rộng để quét cả assembly Application: thêm `AddValidatorsFromAssemblyContaining<TourKit.Application.Customers.CreateCustomerValidator>()`.
  - Pipeline: `app.UseMiddleware<ExceptionHandlingMiddleware>();` (đặt SAU UseExceptionHandler/StatusCodePages hoặc thay chúng — đặt sớm, sau UseSerilogRequestLogging, trước UseAuthentication để bắt cả lỗi downstream).
  - `app.MapControllers();` (cạnh các Map*Endpoints còn lại).
  - BỎ dòng `app.MapCustomerEndpoints();`.
- [ ] **Step 4: Xoá module CQRS Customers** (`Customers/CustomerEndpoints.cs`, `CustomerContracts.cs`, `Customers/Features/*`). Giữ `Permissions.Customer*` (dùng chung).
- [ ] **Step 5: Build 0/0.**
- [ ] **Step 6: Commit** `feat(arch): CustomersController + global exception handler + DI (module mẫu)`.

---

## Task 5: Tests — service unit + controller integration + arch

**Files:**
- Xoá `tests/TourKit.UnitTests/Customers/CustomerSlicesTests.cs` (CQRS cũ). Thêm `tests/TourKit.UnitTests/Customers/CustomerServiceTests.cs` (mock `IRepository<Customer>` + validators).
- Sửa integration test Customers (nếu có trong `tests/TourKit.Tests`) — endpoint/route giữ nguyên `/api/v1/customers` nên test HTTP hầu như không đổi; chạy để xác nhận.
- `tests/TourKit.ArchTests/LayeringTests.cs` — thêm: Application KHÔNG phụ thuộc Infrastructure/Api; Api có thể phụ thuộc Application; Domain(Shared) không dính EF.

- [ ] **Step 1: `CustomerServiceTests`** — dùng một fake `IRepository<Customer>` in-memory (list nội bộ) hoặc Moq (nếu có package; nếu không, viết fake class). Test: Create trả DTO + lưu; Get id lạ → `NotFoundException`; Create FullName rỗng → `ValidationAppException`; Update id lạ → NotFound.
- [ ] **Step 2: Arch test** thêm:
```csharp
private static readonly Assembly Application = typeof(TourKit.Application.Customers.ICustomerService).Assembly;

[Fact] public void Application_khong_phu_thuoc_Infrastructure_Api()
{
    var r = Types.InAssembly(Application).ShouldNot()
        .HaveDependencyOnAny("TourKit.Infrastructure", "TourKit.Api").GetResult();
    Assert.True(r.IsSuccessful, Fail(r));
}
```
- [ ] **Step 3: `dotnet test` full xanh.** Đếm lại (Customers CQRS unit tests thay bằng service tests; integration Customers giữ).
- [ ] **Step 4: Commit** `test(arch): CustomerService unit + arch test tầng Application`.

---

## Kết thúc: DỪNG cho chủ dự án review

Sau Task 5: build 0/0, test xanh, `/api/v1/customers` chạy y hệt (Controller/Service/Repository/Exception). **Không nhân rộng vội** — trình bày module mẫu để chủ dự án review pattern, rồi mới viết plan nhân rộng 10 module còn lại + xoá kernel CQRS.

## Self-Review
- **Đáp ứng yêu cầu:** Controller (MVC) → Service → Repository generic → EF; Exception + global handler; phân tầng project rõ; monolith. ✔
- **Giữ hạ tầng:** đa tenant/JWT/RBAC/SQLite-config/converter/migrate-on-startup không đổi. ✔
- **Không phá 10 module CQRS:** chỉ đổi namespace entity (Task 1) — chúng vẫn chạy tới khi nhân rộng. ✔
- **Route bất biến:** `/api/v1/customers` giữ nguyên → frontend không cần sửa. ✔
- **Rủi ro:** Task 1 đụng ~186 file (cơ học, compiler + 149 test bảo chứng). ReceiptQueries nếu dính EF thì ở lại Infrastructure.
