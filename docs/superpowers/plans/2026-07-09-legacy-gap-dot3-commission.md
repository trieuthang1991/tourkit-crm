# Đợt 3 — Hoa hồng sales + lợi nhuận theo nhân viên — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:subagent-driven-development / executing-plans. Steps dùng checkbox.

**Goal:** Bổ sung **hoa hồng sales** (bám legacy `Comission`: user → %) tách bạch với "chia lợi nhuận" (`ProfitShare` = legacy `ProfitSharing`, giữ nguyên): gắn sales phụ trách đơn (`Order.SalesUserId`), cấu hình rule hoa hồng theo user, và báo cáo **hoa hồng/lợi nhuận theo nhân viên** (legacy `ReportTurnoverProfit`).

**Architecture:** Vertical Slice/CQRS. `CommissionRule` là module CRUD phân trang (mirror Customers). `Order.SalesUserId` nullable + endpoint gán sales. Report mirror `TurnoverReport` nhưng gom theo `SalesUserId`. Công thức: `OrderMath` (cost/profit). SQLite-safe (group/sum ở SQL, ghép ở memory).

**Tech Stack:** .NET 9, EF Core 9 (migration), xUnit InMemory, React 18 + Ant Design.

**Ghi chú phạm vi:** chiều "theo loại khách" (`Comission.id_customer_type`) và `CommissionCampaign` DEFERRED — `Customer` hiện chưa có `CustomerType`. Đợt 3 làm rule theo user (đủ dùng), ghi rõ deferral.

---

## Mẫu nhân bản (đọc trước)

- CRUD module: `src/TourKit.Api/Customers/{CustomerContracts.cs,CustomerEndpoints.cs,Features/*}` + `Persistence/Configurations/CustomerConfiguration.cs`.
- Report: `src/TourKit.Api/Reports/Features/TurnoverReport.cs` + `ReportEndpoints.cs`.
- Domain: `src/TourKit.Infrastructure/Domain/OrderMath.cs`.
- Order: `src/TourKit.Infrastructure/Entities/Order.cs`; booking `Booking/Features/{CreateBooking,BookingFactory}.cs`; `Booking/BookingContracts.cs` (OrderResponse).
- Permissions: `CommissionView`/`CommissionCreate` (đã có — tái dùng cho rule); thêm `ReportCommissionView`.
- Frontend CRUD: `web/src/features/providers/*` (ResourcePage). Report: `web/src/features/reports/TurnoverReportPage.tsx`. Order detail: `web/src/features/booking/OrderDetailPage.tsx`.

---

## Task 1: `Order.SalesUserId` + migration + lộ ra OrderResponse + endpoint gán sales

**Files:**
- Modify: `src/TourKit.Infrastructure/Entities/Order.cs` (thêm `public Guid? SalesUserId { get; set; }`)
- Modify: `src/TourKit.Api/Booking/BookingContracts.cs` (`OrderResponse` thêm `Guid? SalesUserId`)
- Modify: `src/TourKit.Api/Booking/Features/CreateBooking.cs` (`OrderMapper.ToResponse` thêm `o.SalesUserId`); và mọi nơi khởi tạo `OrderResponse` (grep `new OrderResponse(` / `OrderMapper.ToResponse`) — cập nhật cho khớp arity.
- Create: `src/TourKit.Api/Booking/Features/AssignSales.cs` — `AssignSalesCommand(Guid OrderId, Guid? SalesUserId) : ICommand<OrderResponse>`; handler load order, set `SalesUserId`, save, trả `OrderMapper.ToResponse`. NotFound nếu không thấy.
- Modify: `src/TourKit.Api/Booking/BookingEndpoints.cs` — `PUT /api/v1/orders/{orderId}/sales` body `{ Guid? SalesUserId }`, perm `Permissions.BookingCreate`.
- Migration: `dotnet ef migrations add AddOrderSalesUser`.
- Test: `tests/TourKit.UnitTests/Booking/AssignSalesTests.cs`.

- [ ] **Step 1:** Thêm `SalesUserId` vào `Order`. Grep toàn bộ nơi tạo `OrderResponse` (BookingContracts định nghĩa record; `OrderMapper.ToResponse`, `ListOrders`, `GetOrder` nếu có) → thêm field `SalesUserId` cho khớp. Build để lộ hết chỗ sai arity, sửa hết.
- [ ] **Step 2:** Viết `AssignSales.cs` (mirror một command đơn giản, vd CloseDeparture). Endpoint PUT. Migration `AddOrderSalesUser` (kill process giữ DLL nếu `dotnet ef` báo lock: `netstat -ano | grep :5075` → `taskkill //F //PID`).
- [ ] **Step 3:** Test fail-first (`AssignSalesTests`): tạo order (qua BookingFactory), `AssignSalesHandler` set SalesUserId → đọc lại order thấy đúng; order không tồn tại → NotFound.
- [ ] **Step 4:** Run → PASS + `dotnet test` full xanh, build 0/0.
- [ ] **Step 5:** Commit:
```bash
git add -A
git commit -m "feat(booking): gán sales phụ trách đơn (Order.SalesUserId) + migration"
```

---

## Task 2: Permission báo cáo hoa hồng

**Files:** Modify `src/TourKit.Api/Authz/Permissions.cs`
- [ ] Thêm const `ReportCommissionView = "report.commission.view"` + entry `(ReportCommissionView, "Report")` vào `All`.
- [ ] Build + commit `feat(authz): permission báo cáo hoa hồng theo nhân viên`.

---

## Task 3: CommissionRule — CRUD phân trang (mirror Customers)

**Files:**
- Create: `src/TourKit.Infrastructure/Entities/CommissionRule.cs`
- Create: `src/TourKit.Infrastructure/Persistence/Configurations/CommissionRuleConfiguration.cs`
- Modify: `AppDbContext.cs` (DbSet) + migration `AddCommissionRule`
- Create: `src/TourKit.Api/Commission/CommissionRuleContracts.cs` + `Commission/Features/{CreateCommissionRule,UpdateCommissionRule,DeleteCommissionRule,ListCommissionRules}.cs` + `Commission/CommissionRuleEndpoints.cs`
- Modify: `Program.cs` (`app.MapCommissionRuleEndpoints()`)
- Test: `tests/TourKit.UnitTests/Commission/CommissionRuleSlicesTests.cs`

Entity:
```csharp
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Entities;

/// <summary>Quy tắc hoa hồng sales (legacy Comission): 1 user → 1 % hoa hồng trên lợi nhuận đơn.
/// Chiều id_customer_type + CommissionCampaign của legacy DEFERRED (Customer chưa có CustomerType).</summary>
public sealed class CommissionRule : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public decimal Percentage { get; set; }
    public int Status { get; set; }
}
```
Config: `Percentage` `HasPrecision(18,2)`; index `(TenantId, UserId)`.

Contracts:
```csharp
public sealed record CreateCommissionRuleRequest(Guid UserId, decimal Percentage, int Status);
public sealed record UpdateCommissionRuleRequest(decimal Percentage, int Status);
public sealed record CommissionRuleResponse(Guid Id, Guid UserId, decimal Percentage, int Status);
```
Slices mirror Customers (List paged `Paged<CommissionRuleResponse>` ordered by CreatedAt; Create → response; Update `ICommand<bool>`/`Result<bool>` như Customers Update; Delete tương tự). Endpoints under `/api/v1/commission-rules`, perms `CommissionView` (GET) / `CommissionCreate` (POST/PUT/DELETE). List GET nhận `?page=&size=`.

- [ ] **Step 1:** Entity + config + DbSet + migration `AddCommissionRule`.
- [ ] **Step 2:** Test fail-first: validator reject Percentage < 0 (nếu thêm validator — RuleFor GreaterThanOrEqualTo 0); Create→Update→Delete roundtrip (mirror MarketType/Customer slice test).
- [ ] **Step 3:** Viết contracts + 4 slice + endpoints + đăng ký Program.cs.
- [ ] **Step 4:** Run → PASS + full test xanh, build 0/0.
- [ ] **Step 5:** Commit:
```bash
git add -A
git commit -m "feat(commission): CommissionRule — CRUD quy tắc hoa hồng theo user + migration"
```

---

## Task 4: Báo cáo hoa hồng/lợi nhuận theo nhân viên

**Files:**
- Create: `src/TourKit.Api/Reports/Features/CommissionByUserReport.cs`
- Modify: `src/TourKit.Api/Reports/ReportEndpoints.cs`
- Test: `tests/TourKit.UnitTests/Reports/CommissionByUserReportTests.cs`

Contract (bare array):
```csharp
public sealed record CommissionByUserRow(
    Guid UserId, decimal Turnover, decimal Cost, decimal Profit, decimal CommissionР, decimal CommissionAmount);
```
(Đổi `CommissionР` → `CommissionRate`; giữ ASCII.)

Handler `CommissionByUserReportQuery : IQuery<IReadOnlyList<CommissionByUserRow>>`:
- Lấy orders có `SalesUserId != null`: `{ SalesUserId, Id, TotalRevenue }`.
- Cost theo order = Σ `OrderCost.ActualAmount` (group by OrderId → dictionary).
- Gom theo `SalesUserId`: `Turnover = Σ revenue`, `Cost = Σ cost`, `Profit = Turnover - Cost`.
- `CommissionRate` = `Percentage` của `CommissionRule` active (`Status==0`? — dùng rule mới nhất/đầu tiên của user; nếu nhiều rule lấy `FirstOrDefault`) — 0 nếu không có.
- `CommissionAmount = Profit > 0 ? Profit * CommissionRate / 100 : 0`.
- Sort theo `Profit` giảm dần **ở memory**.

Gợi ý:
```csharp
var orders = await _db.Orders.AsNoTracking()
    .Where(o => o.SalesUserId != null)
    .Select(o => new { o.Id, o.TotalRevenue, UserId = o.SalesUserId!.Value }).ToListAsync(ct);
var costByOrder = (await _db.OrderCosts.GroupBy(c => c.OrderId)
    .Select(g => new { OrderId = g.Key, Cost = g.Sum(x => x.ActualAmount) }).ToListAsync(ct))
    .ToDictionary(x => x.OrderId, x => x.Cost);
var rules = (await _db.CommissionRules.AsNoTracking().ToListAsync(ct))
    .GroupBy(r => r.UserId).ToDictionary(g => g.Key, g => g.First().Percentage);
var rows = orders.GroupBy(o => o.UserId).Select(g =>
{
    var turnover = g.Sum(x => x.TotalRevenue);
    var cost = g.Sum(x => costByOrder.GetValueOrDefault(x.Id, 0m));
    var profit = turnover - cost;
    var rate = rules.GetValueOrDefault(g.Key, 0m);
    var amount = profit > 0m ? profit * rate / 100m : 0m;
    return new CommissionByUserRow(g.Key, turnover, cost, profit, rate, amount);
}).OrderByDescending(r => r.Profit).ToList();
```
Route `/api/v1/reports/commission-by-user` (perm `ReportCommissionView`).

- [ ] **Step 1:** Test: seed 2 order cùng SalesUserId (rev 5tr + 5tr), OrderCost 3tr trên 1 order, CommissionRule 10% cho user → row `Turnover=10tr, Cost=3tr, Profit=7tr, CommissionRate=10, CommissionAmount=700k`.
- [ ] **Step 2–4:** FAIL → viết → PASS + full test xanh.
- [ ] **Step 5:** Commit `feat(reports): hoa hồng/lợi nhuận theo nhân viên`.

---

## Task 5: Frontend — cấu hình hoa hồng + báo cáo + gán sales

**Files:**
- Create: `web/src/features/commission/{commissionRuleTypes.ts,commissionRulesCrud.ts,CommissionRulesPage.tsx}`
- Create: `web/src/features/reports/{commissionByUserApi.ts,CommissionByUserReportPage.tsx}`
- Modify: `web/src/features/booking/OrderDetailPage.tsx` (ô gán SalesUserId)
- Modify: `web/src/features/booking/{orderApi hoặc bookingApi}.ts` (hook assign sales)
- Modify: `web/src/app/router.tsx` (2 route) + `AppShell.tsx` (2 nav item)

- [ ] **Step 1: CommissionRules CRUD** — mirror `web/src/features/providers` (ResourcePage + makeCrud). Columns UserId/Percentage/Status. Form: UserId (text — không có users endpoint), Percentage (number), Status (number). basePath `/api/v1/commission-rules`. Perms view=`commission.view`, create/update/delete=`commission.create`. Route `/commission-rules`; nav `{key:'/commission-rules',label:'Cấu hình hoa hồng',perm:'commission.view'}`.
- [ ] **Step 2: Báo cáo hoa hồng theo NV** — mirror TurnoverReportPage: `useCommissionByUser()` (bare array Zod), Table UserId/Turnover/Cost/Profit/CommissionRate/CommissionAmount (money(), rate là %). Route `/reports/commission-by-user`; nav `{key:'/reports/commission-by-user',label:'Hoa hồng NV',perm:'report.commission.view'}`.
- [ ] **Step 3: Gán sales trong OrderDetail** — thêm hook `useAssignSales(orderId)` (PUT `/api/v1/orders/{id}/sales` body `{salesUserId}`, invalidate order/commission-by-user). Trong OrderDetailPage: 1 input UserId + nút "Gán sales" (gate `booking.create`), hiển thị SalesUserId hiện tại từ order (nếu có trong dữ liệu order đã load). Dùng `App.useApp()` + `errorMessage`.
- [ ] **Step 4: Verify + commit** — `npm run build && npm run lint && npm run test`.
```bash
git add web/src/features/commission web/src/features/reports web/src/features/booking web/src/app
git commit -m "feat(web): cấu hình hoa hồng + báo cáo hoa hồng NV + gán sales cho đơn"
```

---

## Self-Review

- **Bám hệ cũ:** `CommissionRule`↔`Comission` (user_id/commission_percentage/status); `SalesUserId`=người bán đơn; report=`ReportTurnoverProfit` theo user. `ProfitShare` giữ nguyên (=`ProfitSharing`, khác hoa hồng). ✔
- **Không nhầm lẫn:** hoa hồng (CommissionRule + report) tách khỏi chia lợi nhuận (ProfitShare panel cũ). ✔
- **Công thức một chỗ:** cost/profit qua `OrderMath` logic (Σ ActualAmount). ✔
- **SQLite-safe:** group/sum ở SQL, ghép+sort ở memory. Migration decimal→double converter đã có. ✔
- **Không hồi quy:** mỗi task `dotnet test` full; Task 1 sửa hết arity `OrderResponse`. ✔
- **Deferred (ghi rõ):** hoa hồng theo loại khách (`id_customer_type`) + `CommissionCampaign` — chờ `Customer.CustomerType`; chốt sổ hoa hồng (`StatusComission`/`date_closed`) — sau.
