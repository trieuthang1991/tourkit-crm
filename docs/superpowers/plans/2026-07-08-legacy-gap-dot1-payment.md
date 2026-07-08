# Đợt 1 — Tài chính chi (Payment Voucher + công nợ phải trả NCC) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax.

**Goal:** Bổ sung **phiếu chi** (`PaymentVoucher`) — đối xứng phiếu thu — cho luồng chi tiền trả nhà cung cấp: tạo/duyệt/từ chối phiếu chi + báo cáo **công nợ phải trả NCC**. Bám legacy `N_PaymentVoucher`.

**Architecture:** Vertical Slice/CQRS, mirror module Finance/Receipt hiện có. `PaymentVoucher` gắn `OrderId` + `ProviderId` (người nhận = NCC) + `OrderCostId` tuỳ chọn (dòng chi phí được thanh toán — legacy `Order_Provider_Money_Id`). Ghi nhận dòng tiền qua `IsRecognized` (legacy `IsGhiNhanDongTien`), giống phiếu thu. Công nợ NCC = Σ `OrderCost.ActualAmount` theo provider − Σ phiếu chi đã ghi nhận.

**Tech Stack:** .NET 9, EF Core 9 (migration mới), xUnit InMemory, React 18 + Ant Design.

---

## Mẫu để nhân bản (đọc trước — mirror y hệt)

- Entity: `src/TourKit.Infrastructure/Entities/ReceiptVoucher.cs` + config `.../Persistence/Configurations/ReceiptVoucherConfiguration.cs`.
- Slices: `src/TourKit.Api/Finance/Features/{CreateReceipt,ApproveReceipt,RejectReceipt,ListReceipts}.cs`.
- Endpoints: `src/TourKit.Api/Finance/ReceiptEndpoints.cs`.
- Report mẫu: `src/TourKit.Api/Reports/Features/OrderDebtReport.cs` + `Reports/ReportEndpoints.cs` (công nợ phải THU → nhân bản sang phải TRẢ).
- Test mẫu: `tests/TourKit.UnitTests/**` (helper `FixedTenant`/`NewDb`) + `tests/TourKit.Tests/Finance/ReceiptEndpointTests.cs`.
- Permissions: `src/TourKit.Api/Authz/Permissions.cs` (`ReceiptView/Create/Approve`, `ReportDebtView`, list `All`).
- Program.cs đăng ký endpoints: tìm `app.MapReceiptEndpoints()` để thêm `app.MapPaymentEndpoints()`.
- Frontend mẫu: `web/src/features/finance/{financeApi.ts,ReceiptsPanel.tsx,receiptTypes.ts}` + `web/src/features/reports/{reportApi.ts,OrderDebtReportPage.tsx}` + mount vào `web/src/features/booking/OrderDetailPage.tsx`.

Legacy `N_PaymentVoucher` (đối chiếu tên): `Order_Id`, `Voucher_Code`, `Voucher_Title`, `Voucher_Dttm`, `Payment_Method`, `Payment_Money`, `Receiver_Name`, `Phone_Number`, `Partner`, `Note`, `Status`, `StatusClose`, `Order_Provider_Money_Id` (→ `OrderCostId`), `IsGhiNhanDongTien` (→ `IsRecognized`), `ParentId`. (Cột signature/fileUpload/TourGuideId/IsAutoChuyen… deferred.)

---

## Task 1: Entity + cấu hình + migration `PaymentVoucher`

**Files:**
- Create: `src/TourKit.Infrastructure/Entities/PaymentVoucher.cs`
- Create: `src/TourKit.Infrastructure/Persistence/Configurations/PaymentVoucherConfiguration.cs`
- Modify: `src/TourKit.Infrastructure/Persistence/AppDbContext.cs` (thêm `DbSet<PaymentVoucher>`)
- Migration: `dotnet ef migrations add AddPaymentVoucher`

- [ ] **Step 1: Entity** `PaymentVoucher.cs`

```csharp
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Entities;

/// <summary>
/// Phiếu chi (legacy N_PaymentVoucher, subset) — tiền công ty chi trả cho NCC theo một đơn.
/// Đối xứng ReceiptVoucher (phiếu thu). Ghi nhận dòng tiền qua IsRecognized (legacy IsGhiNhanDongTien).
/// Legacy còn signature/fileUpload/TourGuideId/IsAutoChuyen/ParentId chuyển tiếp — deferred.
/// </summary>
public sealed class PaymentVoucher : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Title { get; set; }
    public DateTimeOffset IssuedAt { get; set; }

    public Guid OrderId { get; set; }
    public Guid? ProviderId { get; set; }      // người nhận tiền = NCC (legacy Receiver + Order_Provider_Money_Id)
    public Guid? OrderCostId { get; set; }      // dòng chi phí được thanh toán (legacy Order_Provider_Money_Id)

    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string? Partner { get; set; }
    public string? ReceiverName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Note { get; set; }

    public int Status { get; set; }             // 0 = chờ duyệt, 1 = đã duyệt, 2 = từ chối
    public bool IsClosed { get; set; }
    public bool IsRecognized { get; set; }      // legacy IsGhiNhanDongTien — chỉ true khi duyệt
    public Guid? ParentId { get; set; }
}
```

- [ ] **Step 2: Configuration** — mirror `ReceiptVoucherConfiguration.cs` (đọc nó). Điểm bắt buộc: `HasPrecision(18,2)` cho `Amount`; index `(TenantId, OrderId)` và `(TenantId, ProviderId)`; `Code` maxLength hợp lý. Ví dụ:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TourKit.Infrastructure.Entities;

namespace TourKit.Infrastructure.Persistence.Configurations;

public sealed class PaymentVoucherConfiguration : IEntityTypeConfiguration<PaymentVoucher>
{
    public void Configure(EntityTypeBuilder<PaymentVoucher> b)
    {
        b.Property(x => x.Code).HasMaxLength(64);
        b.Property(x => x.Title).HasMaxLength(255);
        b.Property(x => x.PaymentMethod).HasMaxLength(64);
        b.Property(x => x.Partner).HasMaxLength(500);
        b.Property(x => x.ReceiverName).HasMaxLength(255);
        b.Property(x => x.PhoneNumber).HasMaxLength(32);
        b.Property(x => x.Note).HasMaxLength(1000);
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.HasIndex(x => new { x.TenantId, x.OrderId });
        b.HasIndex(x => new { x.TenantId, x.ProviderId });
    }
}
```
Kiểm tra config mẫu xem có cần set `HasQueryFilter` không — KHÔNG cần: `AppDbContext.OnModelCreating` tự gắn filter tenant cho mọi `ITenantEntity`.

- [ ] **Step 3: `DbSet`** trong `AppDbContext.cs`: thêm `public DbSet<PaymentVoucher> PaymentVouchers => Set<PaymentVoucher>();` (theo đúng style các DbSet khác trong file).

- [ ] **Step 4: Migration**

Run: `dotnet ef migrations add AddPaymentVoucher --project src/TourKit.Infrastructure --startup-project src/TourKit.Api`
Kiểm tra file sinh ra tạo bảng `PaymentVouchers` với cột `Amount` REAL/decimal, index như trên. (Dev SQLite dùng converter decimal→double đã có — không cần đụng.)

- [ ] **Step 5: Build + commit**

Run: `dotnet build` → 0/0.
```bash
git add src/TourKit.Infrastructure/Entities/PaymentVoucher.cs src/TourKit.Infrastructure/Persistence/Configurations/PaymentVoucherConfiguration.cs src/TourKit.Infrastructure/Persistence/AppDbContext.cs src/TourKit.Infrastructure/Migrations/*AddPaymentVoucher*
git commit -m "feat(finance): entity PaymentVoucher (phiếu chi) + migration"
```

---

## Task 2: Permissions phiếu chi

**Files:** Modify `src/TourKit.Api/Authz/Permissions.cs`

- [ ] **Step 1:** Thêm hằng (cạnh nhóm Receipt):
```csharp
    public const string PaymentView = "payment.view";
    public const string PaymentCreate = "payment.create";
    public const string PaymentApprove = "payment.approve";
    public const string ReportProviderDebtView = "report.providerdebt.view";
```
- [ ] **Step 2:** Thêm vào `All` (group "Finance"/"Report" cho khớp cách nhóm hiện có):
```csharp
        (PaymentView, "Finance"), (PaymentCreate, "Finance"), (PaymentApprove, "Finance"),
        (ReportProviderDebtView, "Report"),
```
- [ ] **Step 3:** Build + commit
```bash
git add src/TourKit.Api/Authz/Permissions.cs
git commit -m "feat(authz): permission phiếu chi + báo cáo công nợ NCC"
```
(Role Admin khi đăng ký tenant gán TẤT CẢ permission → tự có ngay.)

---

## Task 3: Slices phiếu chi (Create / List / Approve / Reject) + contracts + endpoints

**Files:**
- Create: `src/TourKit.Api/Finance/PaymentContracts.cs`
- Create: `src/TourKit.Api/Finance/Features/{CreatePayment,ListPayments,ApprovePayment,RejectPayment}.cs`
- Create: `src/TourKit.Api/Finance/PaymentEndpoints.cs`
- Modify: `src/TourKit.Api/Program.cs` (đăng ký `app.MapPaymentEndpoints()`)
- Test: `tests/TourKit.UnitTests/Finance/PaymentSlicesTests.cs`

Mirror y hệt Receipt. Contracts:

```csharp
namespace TourKit.Api.Finance;

public sealed record CreatePaymentRequest(
    Guid? ProviderId, Guid? OrderCostId, decimal Amount, string PaymentMethod,
    string? Partner, string? ReceiverName, string? Note);

public sealed record PaymentResponse(
    Guid Id, string Code, Guid OrderId, Guid? ProviderId, Guid? OrderCostId,
    decimal Amount, string PaymentMethod, DateTimeOffset IssuedAt,
    string? Partner, string? ReceiverName, string? Note, int Status, bool IsRecognized);
```

Slice signatures (mirror CreateReceipt/ApproveReceipt/RejectReceipt/ListReceipts — copy logic, đổi entity):
- `CreatePaymentCommand(Guid OrderId, Guid? ProviderId, Guid? OrderCostId, decimal Amount, string PaymentMethod, string? Partner, string? ReceiverName, string? Note) : ICommand<PaymentResponse>` — validate `Amount > 0`; check Order tồn tại (`Error.NotFound`); nếu `ProviderId` có → check provider tồn tại (`Error.Validation` nếu không); tạo `PaymentVoucher` (`Code = "PAY-" + …`, `Title="Phiếu chi"`, `IssuedAt=UtcNow`, `Status=0`, `IsRecognized=false`).
- `ListPaymentsQuery(Guid OrderId) : IQuery<IReadOnlyList<PaymentResponse>>` — bare array; `Result.Success<IReadOnlyList<…>>(list)`.
- `ApprovePaymentCommand(Guid PaymentId) : ICommand<PaymentResponse>` — NotFound nếu không thấy; nếu `Status != 0` → `Error.Conflict("Phiếu đã xử lý.")`; set `Status=1`, `IsRecognized=true`.
- `RejectPaymentCommand(Guid PaymentId) : ICommand<PaymentResponse>` — tương tự, `Status=2`, `IsRecognized=false`.

Endpoints (mirror ReceiptEndpoints):

```csharp
using TourKit.Api.Application;
using TourKit.Api.Authz;
using TourKit.Api.Finance.Features;
using TourKit.Shared.Application;

namespace TourKit.Api.Finance;

public static class PaymentEndpoints
{
    public static IEndpointRouteBuilder MapPaymentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/orders/{orderId:guid}/payments", async (
            Guid orderId, CreatePaymentRequest body, IDispatcher dispatcher, CancellationToken ct) =>
        {
            var cmd = new CreatePaymentCommand(orderId, body.ProviderId, body.OrderCostId, body.Amount,
                body.PaymentMethod, body.Partner, body.ReceiverName, body.Note);
            var result = await dispatcher.Send(cmd, ct);
            return result.Match(p => Results.Created($"/api/v1/orders/{orderId}/payments/{p.Id}", p));
        }).RequireAuthorization(Permissions.PaymentCreate);

        app.MapPost("/api/v1/payments/{paymentId:guid}/approve", async (
            Guid paymentId, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new ApprovePaymentCommand(paymentId), ct))
                .Match(p => Results.Ok(p))).RequireAuthorization(Permissions.PaymentApprove);

        app.MapPost("/api/v1/payments/{paymentId:guid}/reject", async (
            Guid paymentId, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new RejectPaymentCommand(paymentId), ct))
                .Match(p => Results.Ok(p))).RequireAuthorization(Permissions.PaymentApprove);

        app.MapGet("/api/v1/orders/{orderId:guid}/payments", async (
            Guid orderId, IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new ListPaymentsQuery(orderId), ct))
                .Match(list => Results.Ok(list))).RequireAuthorization(Permissions.PaymentView);

        return app;
    }
}
```

- [ ] **Step 1:** Viết `PaymentSlicesTests.cs` fail-first: (a) `CreatePaymentValidator` reject `Amount<=0`; (b) `CreatePaymentHandler` → `NotFound` khi order không tồn tại; (c) create → `ApprovePaymentHandler` set `IsRecognized=true`, approve lần 2 → `Conflict`. Dùng helper `FixedTenant`/`NewDb` (đọc `tests/TourKit.UnitTests/Finance/*` hoặc `Booking/BookingSlicesTests.cs`). Seed Order qua entity `Order`.
- [ ] **Step 2:** Run → FAIL. `dotnet test tests/TourKit.UnitTests --filter PaymentSlicesTests`.
- [ ] **Step 3:** Viết contracts + 4 slice + endpoints (mirror Receipt như trên).
- [ ] **Step 4:** Đăng ký trong `Program.cs`: thêm `app.MapPaymentEndpoints();` ngay dưới `app.MapReceiptEndpoints();`.
- [ ] **Step 5:** Run → PASS + `dotnet test` full xanh. Build 0/0.
- [ ] **Step 6:** Commit
```bash
git add src/TourKit.Api/Finance/PaymentContracts.cs src/TourKit.Api/Finance/Features/*Payment*.cs src/TourKit.Api/Finance/PaymentEndpoints.cs src/TourKit.Api/Program.cs tests/TourKit.UnitTests/Finance/PaymentSlicesTests.cs
git commit -m "feat(finance): phiếu chi — tạo/duyệt/từ chối/liệt kê (CQRS mirror phiếu thu)"
```

---

## Task 4: Báo cáo công nợ phải trả NCC

**Files:**
- Create: `src/TourKit.Api/Reports/Features/ProviderDebtReport.cs`
- Modify: `src/TourKit.Api/Reports/ReportEndpoints.cs` (thêm route)
- Test: `tests/TourKit.UnitTests/Reports/ProviderDebtReportTests.cs`

Logic (đối xứng OrderDebtReport): gom theo `ProviderId`:
- `TotalCost` = Σ `OrderCost.ActualAmount` (theo provider). *(Ghi chú: dùng ActualAmount làm số phải trả; nếu = 0 thì coi như chưa chốt — vẫn cộng như hiện có, không suy diễn thêm.)*
- `Paid` = Σ `PaymentVoucher.Amount` với `ProviderId` khớp và `IsRecognized == true`.
- `Outstanding` = `TotalCost − Paid`.
Chỉ trả provider có `TotalCost > 0` hoặc `Paid > 0`.

Contract row:
```csharp
public sealed record ProviderDebtRow(Guid ProviderId, string ProviderName, decimal TotalCost, decimal Paid, decimal Outstanding);
```

Handler (query, bare array). Gợi ý thực thi (tránh ORDER BY decimal trên SQLite — chỉ group/sum, sort theo tên client-side nếu cần):
```csharp
public sealed record ProviderDebtReportQuery : IQuery<IReadOnlyList<ProviderDebtRow>>;

public sealed class ProviderDebtReportHandler : IQueryHandler<ProviderDebtReportQuery, IReadOnlyList<ProviderDebtRow>>
{
    private readonly AppDbContext _db;
    public ProviderDebtReportHandler(AppDbContext db) => _db = db;

    public async Task<Result<IReadOnlyList<ProviderDebtRow>>> Handle(ProviderDebtReportQuery q, CancellationToken ct)
    {
        var costs = await _db.OrderCosts
            .GroupBy(c => c.ProviderId)
            .Select(g => new { ProviderId = g.Key, Total = g.Sum(x => x.ActualAmount) })
            .ToListAsync(ct);
        var paid = await _db.PaymentVouchers.Where(p => p.IsRecognized && p.ProviderId != null)
            .GroupBy(p => p.ProviderId!.Value)
            .Select(g => new { ProviderId = g.Key, Paid = g.Sum(x => x.Amount) })
            .ToListAsync(ct);
        var providers = await _db.Providers.Select(p => new { p.Id, p.Name }).ToListAsync(ct);

        var ids = costs.Select(c => c.ProviderId).Union(paid.Select(p => p.ProviderId)).Distinct();
        var rows = ids.Select(id =>
        {
            var total = costs.FirstOrDefault(c => c.ProviderId == id)?.Total ?? 0m;
            var pd = paid.FirstOrDefault(p => p.ProviderId == id)?.Paid ?? 0m;
            var name = providers.FirstOrDefault(p => p.Id == id)?.Name ?? id.ToString();
            return new ProviderDebtRow(id, name, total, pd, total - pd);
        }).Where(r => r.TotalCost > 0 || r.Paid > 0).ToList();

        return Result.Success<IReadOnlyList<ProviderDebtRow>>(rows);
    }
}
```
Route trong `ReportEndpoints.cs`:
```csharp
        app.MapGet("/api/v1/reports/provider-debt", async (IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new ProviderDebtReportQuery(), ct))
                .Match(rows => Results.Ok(rows))).RequireAuthorization(Permissions.ReportProviderDebtView);
```

- [ ] **Step 1:** Test fail-first: seed 1 provider + 2 OrderCost (ActualAmount 3tr + 2tr) + 1 PaymentVoucher đã ghi nhận (2tr, ProviderId khớp) → report có 1 row `TotalCost=5tr, Paid=2tr, Outstanding=3tr`. Phiếu chi chưa ghi nhận KHÔNG trừ.
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Viết handler + route.
- [ ] **Step 4:** Run → PASS + `dotnet test` full xanh.
- [ ] **Step 5:** Commit
```bash
git add src/TourKit.Api/Reports/Features/ProviderDebtReport.cs src/TourKit.Api/Reports/ReportEndpoints.cs tests/TourKit.UnitTests/Reports/ProviderDebtReportTests.cs
git commit -m "feat(reports): báo cáo công nợ phải trả nhà cung cấp"
```

---

## Task 5: Frontend — panel phiếu chi + trang công nợ NCC

**Files:**
- Create: `web/src/features/finance/{paymentApi.ts,paymentTypes.ts,PaymentsPanel.tsx}`
- Modify: `web/src/features/booking/OrderDetailPage.tsx` (mount `<PaymentsPanel/>` gate `payment.view`)
- Create: `web/src/features/reports/{providerDebtApi.ts,ProviderDebtReportPage.tsx}`
- Modify: `web/src/app/router.tsx` (route `/reports/provider-debt`) + `web/src/app/AppShell.tsx` (nav item)

- [ ] **Step 1: PaymentsPanel** — mirror `ReceiptsPanel.tsx` (đọc nó). Zod `paymentSchema` (các field PaymentResponse; decimal→z.number(), Guid nullable→`z.string().uuid().nullable()`, IssuedAt→z.string()). Hooks `usePayments(orderId)`, `useCreatePayment(orderId)`, `useApprovePayment(orderId)`, `useRejectPayment(orderId)` (invalidate payments + provider-debt query). Panel: "Tạo phiếu chi" modal (Amount, PaymentMethod, Provider select fed by `providersCrud.useList({page:1,size:200})`, ReceiverName, Note) gate `payment.create`; bảng phiếu (code/amount(money)/method/status/isRecognized) + Approve/Reject gate `payment.approve` khi `status==0`. Dùng `App.useApp()` + `errorMessage`.
- [ ] **Step 2: OrderDetailPage** — thêm `<PaymentsPanel orderId={orderId}/>` cạnh `ReceiptsPanel`, bọc `has('payment.view')`.
- [ ] **Step 3: ProviderDebtReportPage** — mirror `OrderDebtReportPage.tsx`: `useProviderDebt()` (GET `/api/v1/reports/provider-debt`, bare array, Zod), Table (providerName/totalCost/paid/outstanding — `money()`). Route `/reports/provider-debt`; nav item `{ key:'/reports/provider-debt', label:'Công nợ NCC', perm:'report.providerdebt.view' }` trong AppShell.
- [ ] **Step 4: Verify + commit**
Run (trong `web/`): `npm run build && npm run lint && npm run test`.
```bash
git add web/src/features/finance web/src/features/reports web/src/features/booking/OrderDetailPage.tsx web/src/app/router.tsx web/src/app/AppShell.tsx
git commit -m "feat(web): phiếu chi (order detail) + báo cáo công nợ NCC"
```

---

## Self-Review (author checklist)

- **Bám hệ cũ:** `PaymentVoucher` ↔ `N_PaymentVoucher` (Code/Method/Amount/Receiver/Partner/Status/IsRecognized/OrderCostId=Order_Provider_Money_Id). Không thêm cột ngoài legacy. ✔
- **Đối xứng phiếu thu:** create/approve/reject/list mirror Receipt; `IsRecognized` chỉ true khi duyệt (giống receipt) → công nợ chỉ trừ tiền đã ghi nhận. ✔
- **SQLite-safe:** report không ORDER BY decimal/DateTimeOffset (group + sum, ghép client-side). Migration dùng converter decimal→double đã có. ✔
- **RBAC:** 4 permission mới; Admin tự có. ✔
- **Không hồi quy:** mỗi task chạy `dotnet test` full. ✔
- **Ngoài phạm vi (ghi rõ):** duyệt chi NHIỀU CẤP (mirror ReceiptApproval multi-level) chưa làm — Đợt 1 chỉ duyệt 1 cấp như phiếu thu cơ bản; nâng cấp sau nếu cần. Dự trù chi phí tour (`DuTruTours`) tách sang việc sau.
