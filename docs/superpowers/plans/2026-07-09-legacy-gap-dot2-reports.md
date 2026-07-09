# Đợt 2 — Báo cáo & Dashboard — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:subagent-driven-development hoặc superpowers:executing-plans. Steps dùng checkbox `- [ ]`.

**Goal:** Bổ sung 3 báo cáo quản trị hằng ngày (bám legacy SystemReport): **Dashboard tổng quan** (BusinessActivity/HomePage), **Dòng tiền theo phương thức thanh toán** (ReportCashFlow / ListPaymentMethodsCashFlow), **Doanh thu–lợi nhuận theo đơn** (ReportTurnover/TurnoverProfit).

**Architecture:** Vertical Slice/CQRS, mirror `Reports/Features/OrderDebtReport.cs`. Tất cả tính động bằng query, **SQLite-safe**: chỉ GROUP/SUM ở SQL, ghép + sort ở memory (KHÔNG ORDER BY decimal/DateTimeOffset ở SQL). Dùng công thức gom một chỗ: `OrderMath` (chi phí/lợi nhuận), `ReceiptQueries.Recognized()` (phiếu thu đã ghi nhận).

**Tech Stack:** .NET 9, EF Core 9, xUnit InMemory, React 18 + Ant Design.

---

## Mẫu nhân bản (đọc trước)

- `src/TourKit.Api/Reports/Features/{OrderDebtReport.cs,ProviderDebtReport.cs}` + `Reports/ReportEndpoints.cs` + `Reports/ReportContracts.cs` (nếu có).
- `src/TourKit.Infrastructure/Domain/{OrderMath.cs,ReceiptQueries.cs}` — `OrderMath.TotalCost(costs)`, `ReceiptVouchers.Recognized()`.
- Phiếu chi: `PaymentVoucher.IsRecognized` (lọc `.Where(p => p.IsRecognized)`).
- Permissions: `src/TourKit.Api/Authz/Permissions.cs` (`ReportDebtView`, `ReportProviderDebtView`, list `All`).
- Test mẫu: `tests/TourKit.UnitTests/Reports/{OrderDebtReportTests.cs,ProviderDebtReportTests.cs}` (helper `FixedTenant`/`NewDb`).
- Frontend: `web/src/features/reports/{reportApi.ts,OrderDebtReportPage.tsx,providerDebtApi.ts,ProviderDebtReportPage.tsx}`; `web/src/app/{router.tsx,AppShell.tsx}`; `web/src/shared/format.ts` (`money`).

---

## Task 1: Permissions báo cáo

**Files:** Modify `src/TourKit.Api/Authz/Permissions.cs`

- [ ] Thêm const (cạnh `ReportProviderDebtView`):
```csharp
    public const string ReportDashboardView = "report.dashboard.view";
    public const string ReportCashFlowView = "report.cashflow.view";
    public const string ReportTurnoverView = "report.turnover.view";
```
- [ ] Thêm vào `All` (group "Report"):
```csharp
        (ReportDashboardView, "Report"), (ReportCashFlowView, "Report"), (ReportTurnoverView, "Report"),
```
- [ ] Build 0/0 + commit:
```bash
git add src/TourKit.Api/Authz/Permissions.cs
git commit -m "feat(authz): permission dashboard + dòng tiền + doanh thu"
```

---

## Task 2: Báo cáo Dashboard tổng quan

**Files:**
- Create: `src/TourKit.Api/Reports/Features/DashboardReport.cs`
- Modify: `src/TourKit.Api/Reports/ReportEndpoints.cs`
- Test: `tests/TourKit.UnitTests/Reports/DashboardReportTests.cs`

Contract (một object, KHÔNG mảng):
```csharp
public sealed record DashboardSummary(
    int OrderCount,
    decimal TotalRevenue, decimal TotalReceived, decimal ReceivableOutstanding,
    decimal TotalCost, decimal TotalPaid, decimal PayableOutstanding,
    decimal GrossProfit);
```

Handler `DashboardReportQuery : IQuery<DashboardSummary>`:
- `OrderCount` = số Order; `TotalRevenue` = Σ `Order.TotalRevenue`.
- `TotalReceived` = Σ `ReceiptVouchers.Recognized().Sum(Amount)`.
- `TotalCost` = Σ `OrderCosts.Sum(ActualAmount)`.
- `TotalPaid` = Σ `PaymentVouchers.Where(IsRecognized).Sum(Amount)`.
- `ReceivableOutstanding = TotalRevenue - TotalReceived`; `PayableOutstanding = TotalCost - TotalPaid`; `GrossProfit = TotalRevenue - TotalCost`.

Gợi ý (mỗi tổng 1 truy vấn top-level; SUM rỗng → dùng `?? 0m` hoặc `.SumAsync` trả 0):
```csharp
var totalRevenue = await _db.Orders.SumAsync(o => o.TotalRevenue, ct);
var orderCount = await _db.Orders.CountAsync(ct);
var totalReceived = await _db.ReceiptVouchers.Recognized().SumAsync(r => r.Amount, ct);
var totalCost = await _db.OrderCosts.SumAsync(c => c.ActualAmount, ct);
var totalPaid = await _db.PaymentVouchers.Where(p => p.IsRecognized).SumAsync(p => p.Amount, ct);
```
(`SumAsync` trên tập rỗng trả 0 — an toàn. KHÔNG có ORDER BY nên SQLite OK.)

Route:
```csharp
        app.MapGet("/api/v1/reports/dashboard", async (IDispatcher dispatcher, CancellationToken ct) =>
            (await dispatcher.Send(new DashboardReportQuery(), ct))
                .Match(s => Results.Ok(s))).RequireAuthorization(Permissions.ReportDashboardView);
```

- [ ] **Step 1:** Test fail-first: seed 1 order (TotalRevenue 5tr) + 1 recognized receipt 2tr + 1 OrderCost ActualAmount 3tr + 1 recognized payment 1tr → summary `TotalRevenue=5tr, TotalReceived=2tr, ReceivableOutstanding=3tr, TotalCost=3tr, TotalPaid=1tr, PayableOutstanding=2tr, GrossProfit=2tr, OrderCount=1`. (Phiếu thu/chi CHƯA ghi nhận không được tính — thêm 1 phiếu chưa duyệt để kiểm.)
- [ ] **Step 2:** Run → FAIL.
- [ ] **Step 3:** Viết handler + route.
- [ ] **Step 4:** Run → PASS + `dotnet test` full xanh, build 0/0.
- [ ] **Step 5:** Commit:
```bash
git add src/TourKit.Api/Reports/Features/DashboardReport.cs src/TourKit.Api/Reports/ReportEndpoints.cs tests/TourKit.UnitTests/Reports/DashboardReportTests.cs
git commit -m "feat(reports): dashboard tổng quan (doanh thu/thu/chi/công nợ/lợi nhuận)"
```

---

## Task 3: Báo cáo dòng tiền theo phương thức thanh toán

**Files:**
- Create: `src/TourKit.Api/Reports/Features/CashFlowReport.cs`
- Modify: `src/TourKit.Api/Reports/ReportEndpoints.cs`
- Test: `tests/TourKit.UnitTests/Reports/CashFlowReportTests.cs`

Contract (bare array):
```csharp
public sealed record CashFlowRow(string PaymentMethod, decimal Inflow, decimal Outflow, decimal Net);
```

Handler `CashFlowReportQuery : IQuery<IReadOnlyList<CashFlowRow>>`:
- `Inflow` theo method = `ReceiptVouchers.Recognized().GroupBy(PaymentMethod).Sum(Amount)`.
- `Outflow` theo method = `PaymentVouchers.Where(IsRecognized).GroupBy(PaymentMethod).Sum(Amount)`.
- Union các method, `Net = Inflow - Outflow`. Sort theo `Net` giảm dần **ở memory** (sau `ToList`).

```csharp
var inflow = await _db.ReceiptVouchers.Recognized()
    .GroupBy(r => r.PaymentMethod).Select(g => new { Method = g.Key, Sum = g.Sum(x => x.Amount) }).ToListAsync(ct);
var outflow = await _db.PaymentVouchers.Where(p => p.IsRecognized)
    .GroupBy(p => p.PaymentMethod).Select(g => new { Method = g.Key, Sum = g.Sum(x => x.Amount) }).ToListAsync(ct);
var methods = inflow.Select(x => x.Method).Union(outflow.Select(x => x.Method)).Distinct();
var rows = methods.Select(m =>
{
    var i = inflow.FirstOrDefault(x => x.Method == m)?.Sum ?? 0m;
    var o = outflow.FirstOrDefault(x => x.Method == m)?.Sum ?? 0m;
    return new CashFlowRow(m, i, o, i - o);
}).OrderByDescending(r => r.Net).ToList();
```
Route `/api/v1/reports/cash-flow` (perm `ReportCashFlowView`).

- [ ] **Step 1:** Test: seed recognized receipts (cash 2tr, bank 1tr) + recognized payment (cash 1tr) → rows: cash `Inflow=2tr,Outflow=1tr,Net=1tr`, bank `Inflow=1tr,Outflow=0,Net=1tr`. Phiếu chưa ghi nhận không tính.
- [ ] **Step 2–4:** FAIL → viết handler+route → PASS + full test xanh.
- [ ] **Step 5:** Commit:
```bash
git add src/TourKit.Api/Reports/Features/CashFlowReport.cs src/TourKit.Api/Reports/ReportEndpoints.cs tests/TourKit.UnitTests/Reports/CashFlowReportTests.cs
git commit -m "feat(reports): dòng tiền theo phương thức thanh toán"
```

---

## Task 4: Báo cáo doanh thu–lợi nhuận theo đơn

**Files:**
- Create: `src/TourKit.Api/Reports/Features/TurnoverReport.cs`
- Modify: `src/TourKit.Api/Reports/ReportEndpoints.cs`
- Test: `tests/TourKit.UnitTests/Reports/TurnoverReportTests.cs`

Contract (bare array):
```csharp
public sealed record TurnoverRow(Guid OrderId, string OrderCode, decimal Revenue, decimal Cost, decimal Profit);
```

Handler `TurnoverReportQuery : IQuery<IReadOnlyList<TurnoverRow>>` — tính Cost từ OrderCost (không tin cột denormalized), mirror OrderDebtReport:
```csharp
var orders = await _db.Orders.AsNoTracking()
    .Select(o => new { o.Id, o.Code, o.TotalRevenue }).ToListAsync(ct);
var costByOrder = (await _db.OrderCosts
        .GroupBy(c => c.OrderId).Select(g => new { OrderId = g.Key, Cost = g.Sum(x => x.ActualAmount) })
        .ToListAsync(ct))
    .ToDictionary(x => x.OrderId, x => x.Cost);
IReadOnlyList<TurnoverRow> rows = orders
    .Select(o => { var cost = costByOrder.GetValueOrDefault(o.Id, 0m);
        return new TurnoverRow(o.Id, o.Code, o.TotalRevenue, cost, o.TotalRevenue - cost); })
    .OrderByDescending(r => r.Revenue).ToList();
return Result.Success(rows);
```
Route `/api/v1/reports/turnover` (perm `ReportTurnoverView`).

- [ ] **Step 1:** Test: seed 1 order (revenue 5tr) + 2 OrderCost (2tr + 1tr) → 1 row `Revenue=5tr, Cost=3tr, Profit=2tr`.
- [ ] **Step 2–4:** FAIL → viết → PASS + full test xanh.
- [ ] **Step 5:** Commit:
```bash
git add src/TourKit.Api/Reports/Features/TurnoverReport.cs src/TourKit.Api/Reports/ReportEndpoints.cs tests/TourKit.UnitTests/Reports/TurnoverReportTests.cs
git commit -m "feat(reports): doanh thu–lợi nhuận theo đơn"
```

---

## Task 5: Frontend — Dashboard + 2 trang báo cáo

**Files:**
- Create: `web/src/features/reports/{dashboardApi.ts,DashboardPage.tsx,cashFlowApi.ts,CashFlowReportPage.tsx,turnoverApi.ts,TurnoverReportPage.tsx}`
- Modify: `web/src/app/router.tsx` (3 route) + `web/src/app/AppShell.tsx` (3 nav item; đưa Dashboard lên đầu) + đổi redirect `/` → `/dashboard`

- [ ] **Step 1: Dashboard** — `dashboardApi.ts` (`useDashboard()`, GET `/api/v1/reports/dashboard`, Zod object schema). `DashboardPage.tsx` = lưới `Card`+`Statistic` (Ant): Doanh thu / Đã thu / Còn phải thu / Chi phí / Đã chi / Còn phải trả / Lợi nhuận gộp / Số đơn — dùng `money()` (trừ Số đơn). Tiêu đề "Tổng quan".
- [ ] **Step 2: CashFlow** — mirror `OrderDebtReportPage.tsx`: `useCashFlow()` (bare array Zod), Table (Phương thức / Thu vào / Chi ra / Ròng — `money()`).
- [ ] **Step 3: Turnover** — mirror OrderDebtReportPage: `useTurnover()`, Table (Mã đơn / Doanh thu / Chi phí / Lợi nhuận — `money()`); Mã đơn link `/orders/{orderId}`.
- [ ] **Step 4: Router + Nav** — route `/dashboard`, `/reports/cash-flow`, `/reports/turnover`. Nav items: `{key:'/dashboard',label:'Tổng quan',perm:'report.dashboard.view'}` (đầu list), `{key:'/reports/cash-flow',label:'Dòng tiền',perm:'report.cashflow.view'}`, `{key:'/reports/turnover',label:'Doanh thu',perm:'report.turnover.view'}`. Đổi `/` redirect sang `/dashboard`.
- [ ] **Step 5: Verify + commit** — trong `web/`: `npm run build && npm run lint && npm run test`.
```bash
git add web/src/features/reports web/src/app/router.tsx web/src/app/AppShell.tsx
git commit -m "feat(web): dashboard tổng quan + báo cáo dòng tiền + doanh thu"
```

---

## Self-Review

- **Bám hệ cũ:** Dashboard=BusinessActivity/HomePage; CashFlow=ListPaymentMethodsCashFlow; Turnover=ReportTurnover/TurnoverProfit. Không bịa report ngoài SystemReport. ✔
- **Công thức một chỗ:** Cost dùng `OrderCost.ActualAmount` (như `OrderMath.TotalCost`); "đã ghi nhận" dùng `Recognized()`/`IsRecognized` (đồng nhất Đợt 1). ✔
- **SQLite-safe:** chỉ GROUP/SUM/COUNT ở SQL, ghép + ORDER BY ở memory. ✔
- **RBAC:** 3 perm mới, Admin tự có. ✔
- **Không hồi quy:** mỗi task chạy `dotnet test` full. ✔
- **Ngoài phạm vi (ghi rõ):** báo cáo theo NV/sale (TurnoverProfit theo user) gắn với hoa hồng → để Đợt 3; ReportByCustomer/ReportAgency/AccountBalance để sau.
