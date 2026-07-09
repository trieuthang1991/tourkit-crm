# Đợt 4 — CSKH sau tour (Customer Care + Đánh giá) — Implementation Plan

> REQUIRED SUB-SKILL: superpowers:subagent-driven-development / executing-plans. Steps checkbox.

**Goal:** Bổ sung chăm sóc khách hàng (`CustomerCare`, bám legacy `Customer_Care`) và đánh giá sau tour (`TourRating`, bám legacy `Rate`). Hai module CRUD phân trang mirror Customers.

**Architecture:** Vertical Slice/CQRS, mirror module Customers. Migration mới. SQLite-safe (list order theo cột không phải decimal — dùng CreatedAt qua converter đã có; nếu order theo DateTimeOffset thì OK vì converter→long).

**Ghi chú phạm vi:** "Gửi thật" Email/SMS/Zalo (chỉ đang ghi log ở Marketing) DEFERRED — cần tích hợp nhà cung cấp gửi (credential/dịch vụ ngoài). Đợt 4 làm phần dữ liệu CSKH + đánh giá.

---

## Mẫu nhân bản (đọc trước)
- `src/TourKit.Api/Customers/{CustomerContracts.cs,CustomerEndpoints.cs,Features/*}` + `Persistence/Configurations/CustomerConfiguration.cs` (paged CRUD, Update/Delete = `ICommand<bool>`/`Result<bool>`).
- `AppDbContext.cs` (DbSet), `Program.cs` (map endpoints), `Authz/Permissions.cs`.
- Frontend: `web/src/features/providers/*` (ResourcePage CRUD), `web/src/app/{router.tsx,AppShell.tsx}`.
- Test: `tests/TourKit.UnitTests/Customers/*` hoặc `Commission/CommissionRuleSlicesTests.cs`.

---

## Task 1: Permissions
Modify `src/TourKit.Api/Authz/Permissions.cs`:
- [ ] Thêm const + `All` (group "CRM"):
```csharp
    public const string CareView = "care.view";
    public const string CareManage = "care.manage";
    public const string RatingView = "rating.view";
    public const string RatingManage = "rating.manage";
```
`All`: `(CareView,"CRM"),(CareManage,"CRM"),(RatingView,"CRM"),(RatingManage,"CRM"),`
- [ ] Build + commit `feat(authz): permission chăm sóc KH + đánh giá tour`.

---

## Task 2: CustomerCare — entity + CRUD phân trang

**Files:**
- `src/TourKit.Infrastructure/Entities/CustomerCare.cs`, `Persistence/Configurations/CustomerCareConfiguration.cs`, DbSet, migration `AddCustomerCare`
- `src/TourKit.Api/Crm/CustomerCareContracts.cs` + `Crm/Features/{CreateCustomerCare,UpdateCustomerCare,DeleteCustomerCare,ListCustomerCares}.cs` + `Crm/CustomerCareEndpoints.cs`
- `Program.cs` (`app.MapCustomerCareEndpoints()`)
- Test `tests/TourKit.UnitTests/Crm/CustomerCareSlicesTests.cs`

Entity (bám `Customer_Care`):
```csharp
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Entities;

/// <summary>Chăm sóc khách hàng (legacy Customer_Care): lịch/nội dung chăm sóc + nhắc hẹn + phản hồi.</summary>
public sealed class CustomerCare : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid CustomerId { get; set; }
    public string Title { get; set; } = string.Empty;   // Care_Title
    public string? Detail { get; set; }                   // Care_Detail
    public DateTimeOffset? RemindAt { get; set; }         // TimeCareRemind
    public string? Feedback { get; set; }                 // Feedback
    public Guid? AssignedToUserId { get; set; }
    public int Status { get; set; }
}
```
Config: `Title` maxLength 255; index `(TenantId, CustomerId)`.

Contracts:
```csharp
public sealed record CreateCustomerCareRequest(Guid CustomerId, string Title, string? Detail, DateTimeOffset? RemindAt, Guid? AssignedToUserId, int Status);
public sealed record UpdateCustomerCareRequest(string Title, string? Detail, DateTimeOffset? RemindAt, string? Feedback, Guid? AssignedToUserId, int Status);
public sealed record CustomerCareResponse(Guid Id, Guid CustomerId, string Title, string? Detail, DateTimeOffset? RemindAt, string? Feedback, Guid? AssignedToUserId, int Status);
```
Slices mirror Customers: List `Paged<CustomerCareResponse>` order by CreatedAt desc; Create validate `Title` NotEmpty + Customer tồn tại (`Error.Validation` nếu không); Update/Delete `ICommand<bool>`. Endpoints `/api/v1/customer-cares`, perms `CareView`(GET)/`CareManage`(POST/PUT/DELETE). GET nhận `?page=&size=`.

- [ ] **Step 1:** entity+config+DbSet+migration.
- [ ] **Step 2:** test fail-first (validator reject Title rỗng; create→update→delete→list roundtrip; create với CustomerId không tồn tại → Validation).
- [ ] **Step 3:** contracts+slices+endpoints+Program.cs.
- [ ] **Step 4:** run PASS + `dotnet test` full xanh, build 0/0.
- [ ] **Step 5:** commit `feat(crm): chăm sóc khách hàng (CustomerCare) — CRUD + migration`.

---

## Task 3: TourRating — đánh giá sau tour

**Files:**
- `src/TourKit.Infrastructure/Entities/TourRating.cs`, config, DbSet, migration `AddTourRating`
- `src/TourKit.Api/Crm/TourRatingContracts.cs` + `Crm/Features/{CreateTourRating,UpdateTourRating,DeleteTourRating,ListTourRatings}.cs` + `Crm/TourRatingEndpoints.cs`
- `Program.cs`
- Test `tests/TourKit.UnitTests/Crm/TourRatingSlicesTests.cs`

Entity (bám `Rate`):
```csharp
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Entities;

/// <summary>Đánh giá sau tour (legacy Rate): số sao + nhận xét theo chuyến, có thể gắn đơn.</summary>
public sealed class TourRating : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid? TourDepartureId { get; set; }   // Rate.TourId
    public Guid? OrderId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public int Stars { get; set; }               // 1..5 (Rate.AvgStar rút gọn)
    public string? Comment { get; set; }
    public int Status { get; set; }
}
```
Config: index `(TenantId, TourDepartureId)`.

Contracts:
```csharp
public sealed record CreateTourRatingRequest(Guid? TourDepartureId, Guid? OrderId, string? CustomerName, string? CustomerPhone, int Stars, string? Comment, int Status);
public sealed record UpdateTourRatingRequest(string? CustomerName, string? CustomerPhone, int Stars, string? Comment, int Status);
public sealed record TourRatingResponse(Guid Id, Guid? TourDepartureId, Guid? OrderId, string? CustomerName, string? CustomerPhone, int Stars, string? Comment, int Status);
```
Slices mirror Customers. Validate `Stars` trong [1,5] (validator `InclusiveBetween(1,5)`). Endpoints `/api/v1/tour-ratings`, perms `RatingView`/`RatingManage`.

- [ ] **Step 1–5:** như Task 2 (entity+migration; test fail-first: validator reject Stars=0 và Stars=6; roundtrip; commit `feat(crm): đánh giá sau tour (TourRating) — CRUD + migration`).

---

## Task 4: Frontend — 2 trang CRUD

**Files:**
- `web/src/features/care/{customerCareTypes.ts,customerCaresCrud.ts,CustomerCaresPage.tsx}`
- `web/src/features/ratings/{tourRatingTypes.ts,tourRatingsCrud.ts,TourRatingsPage.tsx}`
- `web/src/app/router.tsx` (2 route) + `AppShell.tsx` (2 nav item)

- [ ] **Step 1: CustomerCares** — mirror `web/src/features/providers` (ResourcePage+makeCrud). Columns customerId/title/remindAt(dateText)/status. Form: customerId (text uuid — hoặc select từ `customersCrud.useList` nếu tiện), title, detail(TextArea), remindAt (DatePicker → ISO), status(number); update thêm feedback(TextArea). basePath `/api/v1/customer-cares`. Perms view `care.view`, mutate `care.manage`. Route `/customer-cares`; nav `{key:'/customer-cares',label:'Chăm sóc KH',perm:'care.view'}`.
- [ ] **Step 2: TourRatings** — mirror providers. Columns tourDepartureId/customerName/stars/status. Form: tourDepartureId(text uuid|null), orderId(text uuid|null), customerName, customerPhone, stars(number 1-5), comment(TextArea), status. basePath `/api/v1/tour-ratings`. Perms view `rating.view`, mutate `rating.manage`. Route `/tour-ratings`; nav `{key:'/tour-ratings',label:'Đánh giá tour',perm:'rating.view'}`.
- [ ] **Step 3: Verify + commit** — `npm run build && npm run lint && npm run test`.
```bash
git add web/src/features/care web/src/features/ratings web/src/app
git commit -m "feat(web): chăm sóc khách hàng + đánh giá tour"
```

---

## Self-Review
- **Bám hệ cũ:** `CustomerCare`↔`Customer_Care` (Title/Detail/RemindAt/Feedback/Status); `TourRating`↔`Rate` (TourId/Stars/Comment). ✔
- **Mirror Customers:** CRUD phân trang chuẩn, Update/Delete `ICommand<bool>`. ✔
- **SQLite-safe:** list order theo CreatedAt (converter long). ✔
- **Deferred (ghi rõ):** gửi thật Email/SMS/Zalo (tích hợp ngoài); `comment_tours` (bình luận nội bộ theo tour) — sau nếu cần.
