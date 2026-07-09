# Cấu trúc & Quy ước Backend (Kiến trúc phân tầng)

> **RULE bắt buộc** — mọi module mới/refactor PHẢI theo. Mục tiêu: dễ tiếp cận + **không lặp code/function**.
> Kiến trúc: **N-tier monolith** — `Controller → Service → Repository → EF Core`. Lỗi nghiệp vụ = **Exception + global handler**.

## 1. Bốn tầng & chiều phụ thuộc

Phụ thuộc CHỈ hướng vào trong (ngoài → trong). Vi phạm sẽ fail `TourKit.ArchTests`.

```
TourKit.Api            (Presentation)  →  refs Application, Infrastructure
TourKit.Application    (Business)      →  refs Shared
TourKit.Infrastructure (Data)          →  refs Application, Shared
TourKit.Shared         (Domain)        →  KHÔNG ref ai (leaf); KHÔNG dính EF Core
```

## 2. CÁI GÌ ĐỂ ĐÂU (tra bảng này trước khi tạo file)

### TourKit.Shared (Domain — tầng thấp nhất, ai cũng dùng được)
| Loại | Thư mục | Namespace | Ví dụ |
|---|---|---|---|
| **Entity** (bảng DB) | `Shared/Entities/` | `TourKit.Shared.Entities` | `Customer`, `Order`, `BaseEntity`, `ITenantEntity` |
| **Enum nghiệp vụ** | `Shared/Enums/` | `TourKit.Shared.Enums` | `OrderStatus`, `SeatStatus`, `ProviderType`, `LeadStatus` |
| **Công thức / logic miền** | `Shared/Domain/` | `TourKit.Shared.Domain` | `BookingMath`, `OrderMath`, `ReceiptQueries` |
| **Hằng số nghiệp vụ dùng nhiều nơi** | `Shared/Constants/` | `TourKit.Shared.Constants` | `PaginationDefaults` (DefaultPageSize/MaxPageSize) |
| **Interface hạ tầng miền** | `Shared/Tenancy/` | `TourKit.Shared.Tenancy` | `ITenantContext` |

> ⚠️ Shared KHÔNG được `using Microsoft.EntityFrameworkCore` (arch-test chặn). `ReceiptQueries` chỉ dùng `IQueryable`/`System.Linq` nên hợp lệ.

### TourKit.Application (Business logic — Service + DTO + interface)
| Loại | Thư mục | Namespace | Ví dụ |
|---|---|---|---|
| **Hạ tầng chung của tầng App** | `Application/Common/` | `TourKit.Application.Common` | `IRepository<T>`, `PagedResult<T>`, `Exceptions` (AppException...) |
| **Helper cross-cutting tái dùng** | `Application/Common/` | `TourKit.Application.Common` | guard clause, extension map, chuẩn hoá chuỗi |
| **DTO** (request/response) | `Application/<Module>/` | `TourKit.Application.<Module>` | `CustomerDto`, `CreateCustomerDto`, `UpdateCustomerDto` |
| **Interface service** | `Application/<Module>/` | 〃 | `ICustomerService` |
| **Service** (logic) | `Application/<Module>/` | 〃 | `CustomerService` |
| **Validator** (FluentValidation) | `Application/<Module>/` | 〃 | `CreateCustomerValidator` |
| **Repo interface riêng** (query phức tạp) | `Application/<Module>/` hoặc `Common/` | 〃 | `IOrderRepository` |

### TourKit.Infrastructure (Data access)
| Loại | Thư mục | Ví dụ |
|---|---|---|
| **DbContext, cấu hình EF, migration, interceptor** | `Infrastructure/Persistence/` | `AppDbContext`, `*Configuration`, `Migrations/` |
| **Repository impl** | `Infrastructure/Repositories/` | `Repository<T>` (generic), `OrderRepository` (riêng) |

### TourKit.Api (Presentation)
| Loại | Thư mục | Ví dụ |
|---|---|---|
| **Controller** (mỏng) | `Api/Controllers/` | `CustomersController` |
| **Middleware** | `Api/Middleware/` | `ExceptionHandlingMiddleware` |
| **Permission const + policy** | `Api/Authz/` | `Permissions` (const string) |
| **Auth/JWT** | `Api/Auth/` | `JwtOptions`, `JwtTokenService` |
| **Hằng số cấu hình/seed Api** | cạnh nơi dùng | `PlanCatalog` (Billing), section names |

## 3. Enum — quy tắc

- **Mọi enum nghiệp vụ** (trạng thái, loại, kênh...) → `Shared/Enums/`, namespace `TourKit.Shared.Enums`. Một enum = một file cùng tên.
- Backing type mặc định `int`, **giá trị bắt đầu từ 1** (bám hệ cũ), gán số tường minh.
- **Không** khai enum trong file Contracts/DTO/Controller. Enum lỗi kernel (`ErrorType`) và enum Api-nội-bộ (`RegistrationError`) là ngoại lệ — ở tầng tương ứng.
- FE map số → nhãn (enum serialize thành **number**, không phải string).

## 4. Hằng số (const) — quy tắc

- **Đừng rải magic number/string.** Cần dùng ≥2 nơi → tạo hằng.
- Hằng **nghiệp vụ/miền** (page size, ngưỡng, mã mặc định) → `Shared/Constants/`.
- Hằng **quyền** → `Api/Authz/Permissions.cs`. Hằng **cấu hình** (section name, plan code) → cạnh nơi dùng ở Api.
- Ví dụ đã áp: `PaginationDefaults.DefaultPageSize/MaxPageSize` dùng trong `Repository.PageAsync` (thay 20/200).

## 5. CHỨC NĂNG DÙNG CHUNG — luật chống lặp function ⭐

> Trước khi viết một hàm tính toán / format / query lặp lại, **TÌM Ở ĐÂY TRƯỚC**, không tự viết lại.

| Loại logic dùng chung | ĐẶT Ở ĐÂU | Không được |
|---|---|---|
| **Công thức tiền / nghiệp vụ miền** (tổng dòng, chi phí, lợi nhuận, số chỗ) | `Shared/Domain/*Math.cs` — **một chỗ duy nhất** | ❌ tính lại trong service/controller |
| **Query rule tái dùng** (vd "phiếu đã ghi nhận") | `Shared/Domain/*Queries.cs` (extension trên `IQueryable`) | ❌ lặp `.Where(...)` mỗi nơi |
| **Truy cập dữ liệu CRUD** | `IRepository<T>` (generic) | ❌ inject `AppDbContext` thẳng vào service |
| **Query phức tạp/nhiều bảng** | Repo riêng `I<X>Repository` trong Application + impl Infrastructure | ❌ viết LINQ dài lặp trong nhiều service |
| **Map Entity ↔ DTO** | trong Service (private `Map(...)`) hoặc `Mapper` riêng của module | ❌ map lặp ở controller |
| **Validate input** | Validator (FluentValidation) trong Application/<Module> | ❌ `if` kiểm tra rác trong controller/service |
| **Ném lỗi nghiệp vụ** | `throw new NotFoundException/ConflictException/ValidationAppException/ForbiddenException` (`Application/Common/Exceptions.cs`) | ❌ trả mã lỗi thủ công / `Results.NotFound()` trong service |
| **Helper cross-cutting** (normalize, guard) | `Application/Common/` (static class) | ❌ copy-paste hàm tiện ích |

**Nguyên tắc "một chỗ":** đổi công thức/quy tắc → sửa **đúng một file**. Nếu thấy mình sắp copy một đoạn logic sang module thứ hai → dừng lại, đưa nó về Shared/Domain hoặc Application/Common.

## 6. Khuôn một module (mẫu Customers — copy khi tạo module mới)

```
Shared/Entities/<X>.cs                          entity
Shared/Enums/<XStatus>.cs                        enum (nếu có)
Application/<Module>/<X>Dtos.cs                  CustomerDto, CreateXDto, UpdateXDto
Application/<Module>/I<X>Service.cs              interface
Application/<Module>/<X>Service.cs               logic (validate, map, ném exception)
Application/<Module>/<X>Validators.cs            FluentValidation
Infrastructure/Repositories/                     dùng Repository<T> generic (chỉ thêm repo riêng khi query phức tạp)
Api/Controllers/<X>Controller.cs                 [ApiController], mỏng, [Authorize(Permissions.X)]
tests/TourKit.UnitTests/<Module>/<X>ServiceTests.cs   test service (fake IRepository)
```
Luồng chuẩn service: `validate → repo.GetById (?? throw NotFound) → thao tác entity → repo.Save → map DTO`.

## 7. Giữ nguyên (không đổi khi refactor)
Đa tenant (global query filter + timestamp interceptor trong `AppDbContext`) · JWT/RBAC (`[Authorize(Permissions.*)]` = policy) · config provider SQLite↔SqlServer · converter decimal/DateTimeOffset cho SQLite · migrate-on-startup · route `/api/v1/...` bất biến (FE không phải sửa).

## 8. Kiểm thử ép quy ước
`tests/TourKit.ArchTests/LayeringTests.cs`: Shared không dính EF/Infra/Api · Infrastructure không dính Api · **Application không dính Infrastructure/Api**. Thêm rule mới ở đây khi cần siết.

---
*Chuyển tiếp: 10 module cũ còn chạy CQRS (dispatcher/Result) tới khi nhân rộng xong; sau đó xoá kernel `Shared/Application/*` + `Api/Application/Dispatcher`.*
