# Nhân rộng RESTful Controller/Service/Repository — toàn bộ module + xoá CQRS

> REQUIRED SUB-SKILL: superpowers:executing-plans / subagent-driven-development.

**Goal:** Chuyển TẤT CẢ module còn lại từ minimal-API + CQRS (dispatcher/Result/Features) sang **RESTful Controller → Service → Repository** (khuôn Customers). Chuẩn hoá nghiệp vụ, **giữ nguyên route** `/api/v1/...` (FE không sửa), rồi **xoá sạch file thừa** (kernel CQRS + *Endpoints + Features + Contracts).

**Nguyên tắc bất biến:**
- Route + shape JSON **giữ nguyên** (kiểm bằng integration test hiện có + smoke). `PagedResult<T>` và `Paged<T>` cùng serialize `{items,total,page,size}`.
- Theo `docs/conventions/backend-structure.md`: DTO ở `Dtos/`, Validator ở `Validators/`, **không** `CancellationToken`, service viết tường minh (get→null-check→map), lỗi = throw `NotFoundException/ConflictException/ValidationAppException`.
- Công thức tính: dùng `Shared/Domain` (BookingMath/OrderMath/CommissionMath/RefundMath/ReceiptQueries) — KHÔNG viết lại.

## Task 0: Hạ tầng chung trước khi nhân rộng

- [ ] **DI auto-register service** (khỏi khai báo tay 20+ dòng). Trong `Program.cs`, thay các `AddScoped<IXService, XService>()` bằng Scrutor scan assembly Application:
```csharp
builder.Services.Scan(scan => scan.FromAssemblyOf<TourKit.Application.Customers.ICustomerService>()
    .AddClasses(c => c.Where(t => t.Name.EndsWith("Service")))
        .AsImplementedInterfaces().WithScopedLifetime());
```
- [ ] Validators đã auto-scan (`AddValidatorsFromAssemblyContaining<...>`); giữ.
- [ ] Build + test xanh. Commit `chore(arch): auto-register Application services (Scrutor)`.

## Recipe mỗi module (áp cho tất cả)

Cho module `<M>` với entity `<X>` (route `/api/v1/<res>`):
1. **Tạo** `Application/<M>/Dtos/<X>Dtos.cs` (ns `.Dtos`) — `XDto`, `CreateXDto`, `UpdateXDto` (bê từ `*Contracts.cs` cũ, đổi tên Request→Dto).
2. **Tạo** `Application/<M>/Validators/<X>Validators.cs` (ns `.Validators`) — bê rule từ validator trong Features cũ.
3. **Tạo** `Application/<M>/I<X>Service.cs` + `<X>Service.cs` — gộp logic từ các handler Features (Create/Update/Delete/List/Get + action đặc thù). Dùng `IRepository<X>` generic; query phức tạp/nhiều bảng → thêm repo riêng `Infrastructure/Repositories/<X>Repository.cs` + interface ở Application. Lỗi = throw exception. List phân trang → `PagedResult<XDto>` (repo.PageAsync); list con/bare-array → `IReadOnlyList<XDto>`.
4. **Tạo** `Api/Controllers/<X>Controller.cs` `[ApiController][Route("api/v1/<res>")]` — action mỏng gọi service, `[Authorize(Permissions.*)]` GIỮ NGUYÊN quyền cũ. Giữ đúng verb + path + status code (Created/NoContent/Ok) như endpoint cũ.
5. **Xoá** `Api/<M>/<X>Endpoints.cs`, `<X>Contracts.cs`, `<M>/Features/*<X>*.cs` (các handler/command/query của X).
6. **Bỏ** `app.Map<X>Endpoints()` trong Program.cs.
7. **Test**: xoá slice test CQRS cũ (`tests/TourKit.UnitTests/<M>/*SlicesTests.cs`), thêm `<X>ServiceTests.cs` (fake `IRepository`). Integration test (`tests/TourKit.Tests`) GIỮ (route bất biến) — chạy để xác nhận; sửa nếu nó gọi type CQRS đã xoá.

## Thứ tự nhân rộng (batch — mỗi batch build+test xanh + commit)

- [ ] **Batch 1 — Providers**: Provider (CRUD), OrderCost (create/list `/orders/{id}/costs`), ServiceItem (CRUD), ProviderService (CRUD + filter providerId).
- [ ] **Batch 2 — Crm**: Lead (CRUD + `POST /leads/{id}/convert`), CustomerCare (CRUD), TourRating (CRUD).
- [ ] **Batch 3 — Catalog**: TourTemplate (CRUD + `/{id}/itinerary` GET/PUT + `/{id}/price-scenarios` GET/PUT), MarketType (CRUD), TourAssignee (`/tours/{tourId}/assignees` GET/PUT).
- [ ] **Batch 4 — Commission + Billing**: ProfitShare (`/orders/{id}/profit` GET, `/orders/{id}/profit-shares` GET/POST), CommissionRule (CRUD), Billing (`/plans` GET, `/subscription` GET, `/subscription/change-plan` POST).
- [ ] **Batch 5 — Marketing**: Campaign (CRUD + `/{id}/send` POST + `/{id}/logs` GET).
- [ ] **Batch 6 — Finance**: Receipt (`/orders/{id}/receipts` POST/GET, `/receipts/{id}/approve|reject` POST, `/orders/{id}/balance` GET), ReceiptApproval (`/receipts/{id}/approval` POST/GET + `/approval/act` POST), Payment (`/orders/{id}/payments` POST/GET, `/payments/{id}/approve|reject`).
- [ ] **Batch 7 — Reports**: 6 báo cáo (order-debt, provider-debt, dashboard, cash-flow, turnover, commission-by-user) → 1 `ReportsController` + 1 `IReportService`/`ReportService` (query, dùng IRepository hoặc DbContext read-only qua repo riêng). Giữ route.
- [ ] **Batch 8 — Booking**: Departure (CRUD-lite + `/{id}/close`), Booking (bookings/holds/confirm-seat/deposit/cancel/get-seat/orders/lines/assign-sales), Vehicle (CRUD). Lớn nhất; công thức dùng BookingMath.
- [ ] **Batch 9 — Auth/Registration**: đã dùng service (IAuthService/IProvisioningService), chỉ chuyển endpoint → `AuthController`/`RegistrationController`, giữ service. Không có CQRS để xoá.

## Task cuối: XOÁ KERNEL CQRS + file thừa

Sau khi TẤT CẢ batch xong (không còn `IDispatcher`/`ICommand`/`ICommandHandler`/`Result<>` được dùng):
- [ ] Grep xác nhận 0 tham chiếu: `IDispatcher`, `dispatcher.Send`, `ICommandHandler`, `IQueryHandler`, `.Match(`, `Result<`, `Error.NotFound`.
- [ ] Xoá: `src/TourKit.Api/Application/{Dispatcher.cs,ResultHttp.cs}`, `src/TourKit.Shared/Application/{Cqrs.cs,Result.cs,Error.cs}`. Giữ `Paged.cs`? → thay hẳn bằng `PagedResult` ⇒ xoá `Paged.cs` nếu không còn ai dùng (grep `Paged<`/`PageQuery`).
- [ ] Program.cs: bỏ `AddScoped<IDispatcher,Dispatcher>`, bỏ Scrutor scan `ICommandHandler/IQueryHandler`, bỏ mọi `app.Map*Endpoints()` còn sót. Chỉ còn `AddControllers()` + `MapControllers()` + auth/tenancy/seed.
- [ ] Xoá thư mục Features rỗng + *Contracts.cs còn sót.
- [ ] `ErrorType` enum (Shared) — nếu chỉ kernel dùng thì xoá theo.
- [ ] Build 0/0, `dotnet test` full xanh, arch-test xanh. Commit `refactor(arch): xoá kernel CQRS (dispatcher/Result/Features) — hoàn tất chuyển RESTful phân tầng`.
- [ ] Cập nhật `docs/conventions/backend-architecture.md` + `backend-structure.md`: đánh dấu đã bỏ CQRS.

## Verify chung
Sau mỗi batch: `dotnet build` 0/0 + `dotnet test` full xanh (155 hiện tại; số dịch chuyển khi slice-test→service-test). Route bất biến ⇒ integration test là "lưới an toàn". Cuối cùng smoke SQLite vài endpoint chính (customers/orders/reports) xác nhận controller chạy thật.

## Self-Review
- Route/JSON bất biến → FE 46 test không phải sửa. ✔
- Mỗi batch độc lập build+test xanh + commit → review được giữa chừng. ✔
- Công thức đã ở Domain → service chỉ gọi, không lặp. ✔
- Kernel CQRS chỉ xoá ở bước cuối khi 0 tham chiếu. ✔
