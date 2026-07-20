# Gộp UI về Razor Pages (Vuexy) — Kế hoạch thực thi (Đợt 1)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Gộp UI vào chính `TourKit.Api` bằng Razor Pages nền Vuexy (HTML/CSS/jQuery), gọi Application layer in-process, auth cookie song song JWT — hoàn tất lát cắt dọc: khung + đăng nhập + module Khách hàng.

**Architecture:** Thêm `AddRazorPages()` + thư mục `Pages/` + `wwwroot/` (assets Vuexy) vào `TourKit.Api`. PageModel inject thẳng service Application (`ICustomerService`). Xác thực: một **policy scheme** chọn JWT khi có header `Authorization: Bearer` (cho `/api/*`), ngược lại dùng Cookie (cho trang HTML). Cookie mang claim `sub`/`tenant_id`/`email`/`perm` khớp JWT nên tenancy + RBAC + interceptor chạy nguyên.

**Tech Stack:** .NET 9, ASP.NET Core Razor Pages, Bootstrap 5 + jQuery + Vuexy assets, EF Core, xUnit + WebApplicationFactory.

## Global Constraints

- TargetFramework: **net9.0** (kế thừa `Directory.Build.props`).
- Một deployable duy nhất: **không tạo project mới**; mọi thứ nằm trong `src/TourKit.Api`.
- REST cũ `/api/v1/*` **giữ nguyên hành vi** (auth JWT, 75 controller không sửa).
- Claim bắt buộc trong principal (cookie & JWT): `sub`=userId, `tenant_id`=tenantId, `email`, mỗi quyền một claim `perm`. Không remap claim (`MapInboundClaims=false` đã đặt cho JWT).
- Auth policy đã đăng ký theo code quyền bằng `RequireClaim("perm", code)` — cookie phải phát đúng claim `perm`.
- Demo dev: tenant slug `demo-tour`, admin `admin@demo.vn` / `Demo@12345`.
- Trước khi build/test: **kill process giữ DLL** (cổng 5075) để tránh test fail im lặng.
- Trước khi sửa `Program.cs`/`AuthService`: chạy `gitnexus_impact`; trước commit: `gitnexus_detect_changes`.
- `web/` React **giữ nguyên** đợt này (gỡ ở đợt sau).

---

## File Structure

```
src/TourKit.Api/
  Program.cs                         # MODIFY: AddRazorPages, UseStaticFiles, policy+cookie scheme, MapRazorPages, AuthorizeFolder
  Auth/
    ICookieAuthService.cs            # CREATE
    CookieAuthService.cs             # CREATE — verify + dựng ClaimsPrincipal
  Pages/
    _ViewImports.cshtml              # CREATE
    _ViewStart.cshtml                # CREATE
    Index.cshtml(.cs)                # CREATE — redirect / → /Dashboard
    Ping.cshtml(.cs)                 # CREATE (Task 1, tạm để kiểm tra hạ tầng; xoá ở Task cuối nếu muốn)
    Auth/
      Login.cshtml(.cs)              # CREATE
      Logout.cshtml.cs               # CREATE
    Dashboard/
      Index.cshtml(.cs)             # CREATE
    Customers/
      Index.cshtml(.cs)             # CREATE
      Create.cshtml(.cs)            # CREATE
      Edit.cshtml(.cs)              # CREATE
      _Form.cshtml                   # CREATE
      Index.Delete.cshtml.cs         # (handler xoá gộp trong Index.cshtml.cs)
    Shared/
      _Layout.cshtml                 # CREATE (rút gọn từ Vuexy _ContentNavbarLayout)
      _VerticalMenu.cshtml           # CREATE — 19 nhóm
      _Navbar.cshtml                 # CREATE
      _Footer.cshtml                 # CREATE
      _MenuData.cs                   # CREATE — hằng MENU (C#) + helper lọc quyền
  wwwroot/                           # CREATE — assets Vuexy (css/js/fonts/img/vendor)
tests/TourKit.Tests/
  Auth/CookieAuthServiceTests.cs     # CREATE
  Web/RazorPagesSmokeTests.cs        # CREATE
```

---

### Task 1: Bật Razor Pages trong TourKit.Api (hạ tầng tối thiểu)

**Files:**
- Modify: `src/TourKit.Api/Program.cs`
- Create: `src/TourKit.Api/Pages/_ViewImports.cshtml`
- Create: `src/TourKit.Api/Pages/_ViewStart.cshtml`
- Create: `src/TourKit.Api/Pages/Ping.cshtml`
- Create: `src/TourKit.Api/Pages/Ping.cshtml.cs`
- Test: `tests/TourKit.Tests/Web/RazorPagesSmokeTests.cs`

**Interfaces:**
- Produces: một Razor Page ẩn danh `/Ping` trả HTML 200 (dùng chứng minh pipeline Razor + static files hoạt động cùng REST).

- [ ] **Step 1: Viết test smoke (thất bại trước)**

`tests/TourKit.Tests/Web/RazorPagesSmokeTests.cs`:
```csharp
using System.Net;
using TourKit.Tests.Support;

namespace TourKit.Tests.Web;

public class RazorPagesSmokeTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;
    public RazorPagesSmokeTests(AuthTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Ping_page_renders_anonymously()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/Ping");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var html = await res.Content.ReadAsStringAsync();
        Assert.Contains("pong", html, StringComparison.OrdinalIgnoreCase);
    }
}
```

- [ ] **Step 2: Chạy test — kỳ vọng FAIL**

Kill tiến trình giữ DLL trước:
```bash
powershell -Command "Get-Process dotnet,TourKit.Api -ErrorAction SilentlyContinue | Stop-Process -Force"
dotnet test tests/TourKit.Tests --filter RazorPagesSmokeTests -v minimal
```
Kỳ vọng: FAIL (404 — chưa có Razor Pages).

- [ ] **Step 3: Thêm Razor Pages vào Program.cs**

Trong `src/TourKit.Api/Program.cs`, sau `builder.Services.AddControllers();` (dòng 31) thêm:
```csharp
builder.Services.AddRazorPages();
```
Sau `app.UseCors("web");` (dòng 199) và **trước** `app.UseAuthentication();` thêm:
```csharp
app.UseStaticFiles();   // phục vụ wwwroot (assets Vuexy)
```
Sau `app.MapControllers();` (dòng 225) thêm:
```csharp
app.MapRazorPages();
```

- [ ] **Step 4: Tạo file hạ tầng Razor**

`src/TourKit.Api/Pages/_ViewImports.cshtml`:
```cshtml
@namespace TourKit.Api.Pages
@addTagHelper *, Microsoft.AspNetCore.Mvc.TagHelpers
```

`src/TourKit.Api/Pages/_ViewStart.cshtml`:
```cshtml
@{
    Layout = "_Layout";
}
```

`src/TourKit.Api/Pages/Ping.cshtml`:
```cshtml
@page
@model TourKit.Api.Pages.PingModel
@{
    Layout = null;
}
<!DOCTYPE html>
<html><body><h1>pong</h1></body></html>
```

`src/TourKit.Api/Pages/Ping.cshtml.cs`:
```csharp
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TourKit.Api.Pages;

public class PingModel : PageModel
{
    public void OnGet() { }
}
```

- [ ] **Step 5: Chạy test — kỳ vọng PASS**

```bash
powershell -Command "Get-Process dotnet,TourKit.Api -ErrorAction SilentlyContinue | Stop-Process -Force"
dotnet test tests/TourKit.Tests --filter RazorPagesSmokeTests -v minimal
```
Kỳ vọng: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/TourKit.Api/Program.cs src/TourKit.Api/Pages tests/TourKit.Tests/Web
git commit -m "feat(web): bật Razor Pages + static files trong TourKit.Api (trang Ping smoke)"
```

---

### Task 2: Đưa assets Vuexy + layout vào

**Files:**
- Create: `src/TourKit.Api/wwwroot/**` (copy từ zip)
- Create: `src/TourKit.Api/Pages/Shared/_Layout.cshtml`
- Create: `src/TourKit.Api/Pages/Shared/_Navbar.cshtml`
- Create: `src/TourKit.Api/Pages/Shared/_Footer.cshtml`

**Interfaces:**
- Produces: layout `_Layout` dùng chung cho mọi trang (trừ Login), nạp CSS/JS Vuexy từ `wwwroot`.

- [ ] **Step 1: Giải nén assets Vuexy vào thư mục tạm**

```bash
cd "D:/MiGroup/AI/tourkit-crm/tourkit-crm"
rm -rf /tmp/vuexy && mkdir -p /tmp/vuexy
unzip -q aspnet-core-vue.zip "aspnet-core/AspnetCoreFull/wwwroot/*" -d /tmp/vuexy
unzip -q aspnet-core-vue.zip "aspnet-core/AspnetCoreFull/Pages/Layouts/*" -d /tmp/vuexy
ls /tmp/vuexy/aspnet-core/AspnetCoreFull/wwwroot
```
Kỳ vọng: thấy `css/ js/ fonts/ img/ vendor/ scss/ …`.

- [ ] **Step 2: Copy wwwroot vào TourKit.Api**

```bash
cp -r "/tmp/vuexy/aspnet-core/AspnetCoreFull/wwwroot/." "src/TourKit.Api/wwwroot/"
# Bỏ ảnh layout demo không dùng để giảm rác
rm -rf "src/TourKit.Api/wwwroot/img/layouts" 2>/dev/null || true
ls src/TourKit.Api/wwwroot
```

- [ ] **Step 3: Viết `_Layout.cshtml` rút gọn**

Mở tham chiếu `/tmp/vuexy/aspnet-core/AspnetCoreFull/Pages/Layouts/_ContentNavbarLayout.cshtml` và
`Sections/_Styles.cshtml`, `Sections/_ScriptsIncludes.cshtml` để copy đúng đường dẫn asset thực tế.
`src/TourKit.Api/Pages/Shared/_Layout.cshtml` (khung Vuexy tối giản — sửa đường dẫn css/js cho khớp thư mục vừa copy):
```cshtml
@{
    var title = ViewData["Title"] as string ?? "TourKit";
}
<!DOCTYPE html>
<html lang="vi" class="light-style layout-menu-fixed" data-theme="theme-purple" data-assets-path="~/">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>@title · TourKit</title>
    <link rel="stylesheet" href="~/vendor/fonts/boxicons.css" />
    <link rel="stylesheet" href="~/vendor/css/core.css" />
    <link rel="stylesheet" href="~/vendor/css/theme-default.css" />
    <link rel="stylesheet" href="~/css/demo.css" />
    <link rel="stylesheet" href="~/css/tourkit.css" />
    <script src="~/vendor/js/helpers.js"></script>
    <script src="~/js/config.js"></script>
</head>
<body>
<div class="layout-wrapper layout-content-navbar">
  <div class="layout-container">
    <partial name="Shared/_VerticalMenu" />
    <div class="layout-page">
      <partial name="Shared/_Navbar" />
      <div class="content-wrapper">
        <div class="container-xxl flex-grow-1 container-p-y">
          @RenderBody()
        </div>
        <partial name="Shared/_Footer" />
        <div class="content-backdrop fade"></div>
      </div>
    </div>
  </div>
  <div class="layout-overlay layout-menu-toggle"></div>
</div>
<script src="~/vendor/libs/jquery/jquery.js"></script>
<script src="~/vendor/libs/popper/popper.js"></script>
<script src="~/vendor/js/bootstrap.js"></script>
<script src="~/vendor/js/menu.js"></script>
<script src="~/js/main.js"></script>
@await RenderSectionAsync("Scripts", required: false)
</body>
</html>
```
> Lưu ý: tên file trong `~/vendor/...` phải khớp thư mục thực tế vừa copy (Vuexy thường có `vendor/css/core.css`, `vendor/js/bootstrap.js`, `vendor/libs/jquery/jquery.js`, `js/main.js`, `js/config.js`). Nếu khác, sửa đường dẫn cho đúng — kiểm bằng `ls src/TourKit.Api/wwwroot/vendor/js`.

- [ ] **Step 4: Navbar + Footer tối giản**

`src/TourKit.Api/Pages/Shared/_Navbar.cshtml`:
```cshtml
<nav class="layout-navbar container-xxl navbar navbar-expand-xl navbar-detached align-items-center bg-navbar-theme">
  <div class="layout-menu-toggle navbar-nav align-items-xl-center me-3 me-xl-0 d-xl-none">
    <a class="nav-item nav-link px-0 me-xl-4" href="javascript:void(0)"><i class="bx bx-menu bx-sm"></i></a>
  </div>
  <div class="navbar-nav-right d-flex align-items-center" id="navbar-collapse">
    <div class="navbar-nav align-items-center">
      <div class="nav-item d-flex align-items-center">
        <i class="bx bx-search fs-4 lh-0"></i>
        <input type="text" class="form-control border-0 shadow-none" placeholder="Tìm kiếm..." />
      </div>
    </div>
    <ul class="navbar-nav flex-row align-items-center ms-auto">
      <li class="nav-item">
        <form method="post" asp-page="/Auth/Logout">
          <button type="submit" class="btn btn-sm btn-outline-secondary">Đăng xuất</button>
        </form>
      </li>
    </ul>
  </div>
</nav>
```

`src/TourKit.Api/Pages/Shared/_Footer.cshtml`:
```cshtml
<footer class="content-footer footer bg-footer-theme">
  <div class="container-xxl d-flex flex-wrap justify-content-between py-2 flex-md-row flex-column">
    <div class="mb-2 mb-md-0">© @DateTime.Now.Year TourKit</div>
  </div>
</footer>
```

- [ ] **Step 5: File CSS override tím (rỗng, có sẵn để đắp theme)**

`src/TourKit.Api/wwwroot/css/tourkit.css`:
```css
/* Override theme tím TourKit (memory vuexy-theme-adoption). Đắp dần ở đợt sau. */
:root { --bs-primary: #7367f0; }
```

- [ ] **Step 6: Build + chạy thử thủ công**

```bash
powershell -Command "Get-Process dotnet,TourKit.Api -ErrorAction SilentlyContinue | Stop-Process -Force"
dotnet build src/TourKit.Api -v minimal
```
Kỳ vọng: BUILD PASS. (Kiểm tra trực quan để Task 3 khi có menu: `dotnet run --project src/TourKit.Api` rồi mở trang bất kỳ — làm ở Task 6.)

- [ ] **Step 7: Commit**

```bash
git add src/TourKit.Api/wwwroot src/TourKit.Api/Pages/Shared
git commit -m "feat(web): thêm assets Vuexy + layout/navbar/footer (theme tím)"
```

---

### Task 3: Menu dọc 19 nhóm (bám hệ cũ)

**Files:**
- Create: `src/TourKit.Api/Pages/Shared/_MenuData.cs`
- Create: `src/TourKit.Api/Pages/Shared/_VerticalMenu.cshtml`

**Interfaces:**
- Produces: `MenuData.Groups` (`IReadOnlyList<MenuNode>`) và `MenuData.FilterByPerm(nodes, has)`.
- `MenuNode`: `record MenuNode(string Key, string Label, string? Icon, string? Perm, string? To, IReadOnlyList<MenuNode> Children)`.

- [ ] **Step 1: Định nghĩa MENU trong C#**

Nguồn chuẩn: `web/src/app/AppShell.tsx` hằng `MENU` (19 nhóm). Tái tạo **đúng nhãn/thứ tự/lồng/perm**.
`src/TourKit.Api/Pages/Shared/_MenuData.cs`:
```csharp
namespace TourKit.Api.Pages.Shared;

public sealed record MenuNode(
    string Key, string Label, string? Icon = null, string? Perm = null,
    string? To = null, IReadOnlyList<MenuNode>? Children = null);

public static class MenuData
{
    // Bám CHÍNH XÁC menu hệ cũ (AppShell.tsx). To = route thực tế đợt 1 (chỉ /Customers có trang thật;
    // phần còn lại trỏ "#" placeholder, nối dần đợt sau).
    public static readonly IReadOnlyList<MenuNode> Groups = new List<MenuNode>
    {
        new("g-workspace", "Workspace", "bx-bar-chart-alt-2", Children: new List<MenuNode>
        {
            new("w-social", "Mạng Nội Bộ", To: "#", Perm: "post.view"),
            new("w-workspace", "Bàn làm việc", To: "#", Perm: "report.dashboard.view"),
            new("w-dashboard", "Tổng quan", To: "/Dashboard", Perm: "report.dashboard.view"),
            new("w-noti", "Thông báo", To: "#", Perm: "report.dashboard.view"),
        }),
        new("g-provider", "Nhà cung cấp", "bx-store", Children: new List<MenuNode>
        {
            new("p-all", "Tất cả Nhà cung cấp", To: "#", Perm: "provider.view"),
            new("p-services", "Danh mục dịch vụ", To: "#", Perm: "service.view"),
            new("p-pricing", "Bảng giá NCC", To: "#", Perm: "service.view"),
            new("p-terms", "Điều khoản TT NCC", To: "#", Perm: "provider.view"),
            new("p-series", "Series Vé / Quỹ vé", To: "#", Perm: "ticketfund.view"),
        }),
        new("g-crm", "CRM", "bx-group", Children: new List<MenuNode>
        {
            new("crm-share", "Chia số Sale", To: "#", Perm: "lead.view"),
            new("crm-opp", "Cơ hội bán hàng", To: "#", Perm: "lead.view"),
            new("crm-data", "Data khách hàng", To: "/Customers", Perm: "customer.view"),
            new("crm-dedup", "Rà khách trùng", To: "#", Perm: "customer.view"),
            new("crm-care", "Quản lý lịch hẹn", To: "#", Perm: "care.view"),
            new("crm-feedback", "Feedback", Children: new List<MenuNode>
            {
                new("fb-general", "Feedback chung", To: "#", Perm: "rating.view"),
                new("fb-tour", "Feedback theo Tour", To: "#", Perm: "rating.view"),
                new("fb-zns", "Feedback ZNS", To: "#", Perm: "rating.view"),
            }),
        }),
        new("g-quote", "Báo Giá", "bx-calculator", Children: new List<MenuNode>
        {
            new("q-tour", "Tính giá Tour", To: "#", Perm: "quote.view"),
            new("q-combo", "Tính giá Combo", To: "#", Perm: "quote.view"),
            new("q-git", "Tour GIT/Combo", To: "#", Perm: "quote.view"),
            new("q-landtour", "Landtour", To: "#", Perm: "quote.view"),
            new("q-booking", "Booking Phòng", To: "#", Perm: "quote.view"),
            new("q-service", "Dịch vụ lẻ", To: "#", Perm: "quote.view"),
            new("q-visa", "Visa", To: "#", Perm: "quote.view"),
            new("q-agent", "Báo giá Đại lý (B2B)", To: "#", Perm: "agentquote.view"),
        }),
        new("g-order", "Đơn hàng/LKH", "bx-cart", Children: new List<MenuNode>
        {
            new("o-all", "Tất cả đơn hàng", To: "#", Perm: "booking.view"),
            new("o-tours", "Tất cả Tour/LKH", To: "#", Perm: "departure.view"),
            new("o-fit", "Tour FIT", To: "#", Perm: "booking.view"),
            new("o-git", "Tour GIT/Combo", To: "#", Perm: "booking.view"),
            new("o-landtour", "LandTour", To: "#", Perm: "booking.view"),
            new("o-visa", "Visa", To: "#", Perm: "booking.view"),
            new("o-service", "Dịch vụ lẻ", To: "#", Perm: "booking.view"),
        }),
        new("g-booking", "Booking Phòng/Khách sạn", "bx-hotel", Children: new List<MenuNode>
        {
            new("b-roomfund", "Quỹ phòng", To: "#", Perm: "roomfund.view"),
            new("b-list", "Danh sách Booking", To: "#", Perm: "servicebooking.view"),
            new("b-roomclass", "Hạng phòng (danh mục)", To: "#", Perm: "servicebooking.view"),
        }),
        new("g-flight", "Vé Máy Bay", "bx-plane", Children: new List<MenuNode>
        {
            new("f-provider", "Nhà cung cấp vé", To: "#", Perm: "provider.view"),
            new("f-group", "Vé máy bay đoàn", To: "#", Perm: "ticketfund.view"),
            new("f-individual", "Vé máy bay lẻ", To: "#", Perm: "ticketfund.view"),
        }),
        new("g-guide", "Hướng dẫn viên", "bx-id-card", Children: new List<MenuNode>
        {
            new("gd-provider", "Hướng dẫn viên", To: "#", Perm: "guide.view"),
            new("gd-calendar", "Lịch điều Hướng dẫn viên", To: "#", Perm: "guide.view"),
            new("gd-report", "Báo cáo", To: "#", Perm: "guide.view"),
        }),
        new("g-vehicle", "Quản lý xe", "bx-car", Children: new List<MenuNode>
        {
            new("v-store", "Kho xe", To: "#", Perm: "vehicle.view"),
            new("v-waiting", "Lịch xe chờ duyệt", To: "#", Perm: "vehicle.view"),
            new("v-manage", "Lịch điều xe", To: "#", Perm: "vehicle.view"),
            new("v-report", "Báo cáo", To: "#", Perm: "vehicle.view"),
        }),
        new("g-operation", "Điều hành Tour", "bx-task", Children: new List<MenuNode>
        {
            new("op-voucher", "Phiếu điều hành dịch vụ", To: "#", Perm: "servicebooking.view"),
            new("op-calendar", "Lịch điều hành", To: "#", Perm: "departure.view"),
        }),
        new("g-finance", "Tài chính/Kế toán", "bx-bank", Children: new List<MenuNode>
        {
            new("fi-waiting", "Phiếu thu chờ", To: "#", Perm: "receipt.view"),
            new("fi-receipt", "Phiếu thu", To: "#", Perm: "receipt.view"),
            new("fi-payment", "Phiếu chi", To: "#", Perm: "payment.view"),
            new("fi-invoice", "Danh sách hoá đơn (VAT)", To: "#", Perm: "invoice.view"),
            new("fi-cashflow", "Thống kê dòng tiền", To: "#", Perm: "report.cashflow.view"),
            new("fi-debt-c", "Công nợ khách", To: "#", Perm: "report.debt.view"),
            new("fi-debt-p", "Công nợ NCC", To: "#", Perm: "report.providerdebt.view"),
        }),
        new("g-kpi", "KPIs", "bx-trending-up", Children: new List<MenuNode>
        {
            new("kpi-config", "Thiết lập KPIs", To: "#", Perm: "report.dashboard.view"),
        }),
        new("g-commission", "Hoa Hồng", "bx-percentage", Children: new List<MenuNode>
        {
            new("hh-config", "Thiết lập hoa hồng", To: "#", Perm: "commission.view"),
            new("hh-campaign", "Chính sách hoa hồng (bậc thang)", To: "#", Perm: "commission.view"),
            new("hh-customer", "HH theo loại khách", To: "#", Perm: "commission.view"),
            new("hh-source", "Báo cáo theo nguồn", To: "#", Perm: "report.commission.view"),
            new("hh-milestone", "Báo cáo theo cột mốc", To: "#", Perm: "report.commission.view"),
        }),
        new("g-project", "Dự án & Công việc", "bx-list-check", Children: new List<MenuNode>
        {
            new("pj-project", "Dự án", To: "#", Perm: "workflow.view"),
            new("pj-mytask", "Công việc của tôi", To: "#", Perm: "task.view"),
            new("pj-tasks", "Danh sách Công việc", To: "#", Perm: "task.view"),
            new("pj-perf", "Báo cáo Hiệu suất", To: "#", Perm: "task.view"),
        }),
        new("g-marketing", "Marketing", "bx-broadcast", Children: new List<MenuNode>
        {
            new("mkt-email", "Email Marketing", Children: new List<MenuNode>
            {
                new("mkt-campaign", "Chiến dịch", To: "#", Perm: "marketing.view"),
                new("mkt-store", "Kho Email Mẫu", To: "#", Perm: "marketing.view"),
            }),
            new("mkt-zalo", "Zalo OA/ZBS", Children: new List<MenuNode>
            {
                new("zalo-oa", "Thông tin OA", To: "#", Perm: "marketing.view"),
                new("zalo-zns", "ZNS", To: "#", Perm: "marketing.view"),
                new("zalo-uid", "Zalo UID (Tin follow OA)", To: "#", Perm: "marketing.view"),
            }),
            new("mkt-posts", "Bài viết", To: "#", Perm: "post.view"),
            new("mkt-postcat", "Chuyên mục bài viết", To: "#", Perm: "post.view"),
        }),
        new("g-report", "Báo cáo", "bx-bar-chart", Children: new List<MenuNode>
        {
            new("rp-seller", "Nhân viên", To: "#", Perm: "report.turnover.view"),
            new("rp-money", "Tài chính", To: "#", Perm: "report.turnover.view"),
            new("rp-tourtype", "Thu chi theo loại tour", To: "#", Perm: "report.turnover.view"),
            new("rp-export", "Xuất báo cáo", To: "#", Perm: "report.turnover.view"),
            new("rp-system", "Báo cáo tổng hợp", To: "#", Perm: "report.turnover.view"),
        }),
        new("g-agent", "Đại lý (B2B)", "bx-handshake", Children: new List<MenuNode>
        {
            new("ag-list", "Danh sách đại lý", To: "#", Perm: "agent.view"),
            new("ag-booking", "Đặt chỗ đại lý", To: "#", Perm: "agentquote.view"),
        }),
        new("g-system", "Cài đặt hệ thống", "bx-cog", Children: new List<MenuNode>
        {
            new("sys-users", "Thành viên", To: "#", Perm: "user.view"),
            new("sys-roles", "Vai trò & quyền", To: "#", Perm: "user.view"),
            new("sys-config", "Cấu hình", To: "#", Perm: "user.view"),
            new("sys-billing", "Gói dịch vụ", To: "#", Perm: "subscription.view"),
        }),
        new("g-log", "Log hệ thống", "bx-history", Children: new List<MenuNode>
        {
            new("log-system", "Log hệ thống", To: "#", Perm: "activitylog.view"),
        }),
    };

    /// <summary>Lọc theo quyền: bỏ leaf thiếu perm, bỏ nhóm rỗng sau khi lọc.</summary>
    public static IReadOnlyList<MenuNode> FilterByPerm(IReadOnlyList<MenuNode> nodes, Func<string, bool> has)
    {
        var result = new List<MenuNode>();
        foreach (var n in nodes)
        {
            if (n.Children is { Count: > 0 })
            {
                var kids = FilterByPerm(n.Children, has);
                if (kids.Count > 0) result.Add(n with { Children = kids });
            }
            else if (n.Perm is null || has(n.Perm))
            {
                result.Add(n);
            }
        }
        return result;
    }
}
```

- [ ] **Step 2: Render menu đệ quy**

`src/TourKit.Api/Pages/Shared/_VerticalMenu.cshtml`:
```cshtml
@using TourKit.Api.Pages.Shared
@{
    bool Has(string p) => User.HasClaim("perm", p);
    var groups = MenuData.FilterByPerm(MenuData.Groups, Has);
    var current = ViewContext.HttpContext.Request.Path.Value ?? "/";
}
@functions {
    bool IsActive(MenuNode n, string path) =>
        n.To is { } to && to != "#" && (path.Equals(to, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(to + "/", StringComparison.OrdinalIgnoreCase));
    bool GroupActive(MenuNode g, string path) =>
        g.Children?.Any(c => IsActive(c, path) || GroupActive(c, path)) ?? false;
}
<aside id="layout-menu" class="layout-menu menu-vertical menu bg-menu-theme">
  <div class="app-brand demo">
    <a href="/Dashboard" class="app-brand-link">
      <span class="app-brand-logo demo"><b>T</b></span>
      <span class="app-brand-text demo menu-text fw-bold">TourKit</span>
    </a>
  </div>
  <ul class="menu-inner py-1">
    @foreach (var g in groups)
    {
        var gActive = GroupActive(g, current);
        <li class="menu-item @(gActive ? "active open" : "")">
            <a href="javascript:void(0)" class="menu-link menu-toggle">
                @if (g.Icon != null) { <i class="menu-icon tf-icons bx @g.Icon"></i> }
                <div>@g.Label</div>
            </a>
            <ul class="menu-sub">
                @foreach (var c in g.Children!)
                {
                    if (c.Children is { Count: > 0 })
                    {
                        <li class="menu-item @(GroupActive(c, current) ? "active open" : "")">
                            <a href="javascript:void(0)" class="menu-link menu-toggle"><div>@c.Label</div></a>
                            <ul class="menu-sub">
                                @foreach (var sub in c.Children)
                                {
                                    <li class="menu-item @(IsActive(sub, current) ? "active" : "")">
                                        <a href="@sub.To" class="menu-link"><div>@sub.Label</div></a>
                                    </li>
                                }
                            </ul>
                        </li>
                    }
                    else
                    {
                        <li class="menu-item @(IsActive(c, current) ? "active" : "")">
                            <a href="@c.To" class="menu-link"><div>@c.Label</div></a>
                        </li>
                    }
                }
            </ul>
        </li>
    }
  </ul>
</aside>
```

- [ ] **Step 3: Build**

```bash
powershell -Command "Get-Process dotnet,TourKit.Api -ErrorAction SilentlyContinue | Stop-Process -Force"
dotnet build src/TourKit.Api -v minimal
```
Kỳ vọng: BUILD PASS.

- [ ] **Step 4: Commit**

```bash
git add src/TourKit.Api/Pages/Shared/_MenuData.cs src/TourKit.Api/Pages/Shared/_VerticalMenu.cshtml
git commit -m "feat(web): menu dọc 19 nhóm bám hệ cũ + lọc theo quyền"
```

---

### Task 4: CookieAuthService (xác thực + dựng ClaimsPrincipal)

**Files:**
- Create: `src/TourKit.Api/Auth/ICookieAuthService.cs`
- Create: `src/TourKit.Api/Auth/CookieAuthService.cs`
- Modify: `src/TourKit.Api/Program.cs` (đăng ký DI)
- Test: `tests/TourKit.Tests/Auth/CookieAuthServiceTests.cs`

**Interfaces:**
- Produces: `ICookieAuthService.AuthenticateAsync(string tenantSlug, string email, string password) : Task<ClaimsPrincipal?>` — trả `ClaimsPrincipal` (scheme cookie) với claim `sub`/`tenant_id`/`email`/`perm`, hoặc `null` nếu sai.
- Consumes: `AppDbContext`, `AmbientTenantContext`, `IPasswordHasher` (đã có).

> Trước khi sửa Program.cs: chạy `gitnexus_impact({target: "Program", direction: "upstream"})` và báo blast radius.

- [ ] **Step 1: Viết test (thất bại trước)**

`tests/TourKit.Tests/Auth/CookieAuthServiceTests.cs`:
```csharp
using System.Security.Claims;
using Microsoft.Extensions.DependencyInjection;
using TourKit.Api.Auth;
using TourKit.Tests.Support;

namespace TourKit.Tests.Auth;

public class CookieAuthServiceTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;
    public CookieAuthServiceTests(AuthTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Valid_credentials_return_principal_with_claims()
    {
        var (slug, email, password) = await _factory.SeedTenantUserAsync("cookie-ok");
        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<ICookieAuthService>();

        var principal = await svc.AuthenticateAsync(slug, email, password);

        Assert.NotNull(principal);
        Assert.False(string.IsNullOrEmpty(principal!.FindFirst("sub")?.Value));
        Assert.False(string.IsNullOrEmpty(principal.FindFirst("tenant_id")?.Value));
        Assert.Equal(email, principal.FindFirst("email")?.Value);
    }

    [Fact]
    public async Task Wrong_password_returns_null()
    {
        var (slug, email, _) = await _factory.SeedTenantUserAsync("cookie-bad");
        using var scope = _factory.Services.CreateScope();
        var svc = scope.ServiceProvider.GetRequiredService<ICookieAuthService>();

        Assert.Null(await svc.AuthenticateAsync(slug, email, "wrong-password"));
    }
}
```

- [ ] **Step 2: Chạy test — kỳ vọng FAIL (không compile: thiếu ICookieAuthService)**

```bash
powershell -Command "Get-Process dotnet,TourKit.Api -ErrorAction SilentlyContinue | Stop-Process -Force"
dotnet test tests/TourKit.Tests --filter CookieAuthServiceTests -v minimal
```
Kỳ vọng: FAIL (build error / DI chưa đăng ký).

- [ ] **Step 3: Tạo interface + implementation**

`src/TourKit.Api/Auth/ICookieAuthService.cs`:
```csharp
using System.Security.Claims;

namespace TourKit.Api.Auth;

public interface ICookieAuthService
{
    /// <summary>Xác thực và dựng ClaimsPrincipal (scheme cookie) hoặc null nếu sai.</summary>
    Task<ClaimsPrincipal?> AuthenticateAsync(string tenantSlug, string email, string password);
}
```

`src/TourKit.Api/Auth/CookieAuthService.cs`:
```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using TourKit.Api.Tenancy;
using TourKit.Infrastructure.Persistence;

namespace TourKit.Api.Auth;

public sealed class CookieAuthService : ICookieAuthService
{
    private readonly AppDbContext _db;
    private readonly AmbientTenantContext _tenant;
    private readonly IPasswordHasher _hasher;

    public CookieAuthService(AppDbContext db, AmbientTenantContext tenant, IPasswordHasher hasher)
    {
        _db = db;
        _tenant = tenant;
        _hasher = hasher;
    }

    public async Task<ClaimsPrincipal?> AuthenticateAsync(string tenantSlug, string email, string password)
    {
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Slug == tenantSlug && !t.IsDeleted);
        if (tenant is null) return null;

        var user = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.TenantId == tenant.Id && u.Email == email && !u.IsDeleted);
        if (user is null || !user.IsActive || !_hasher.Verify(user.PasswordHash, password))
            return null;

        _tenant.SetTenant(tenant.Id);   // để LoadPermissions lọc đúng tenant

        var permissions = await _db.UserRoles.Where(ur => ur.UserId == user.Id)
            .Join(_db.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (ur, rp) => rp.PermissionId)
            .Join(_db.Permissions, pid => pid, p => p.Id, (pid, p) => p.Code)
            .Distinct().ToListAsync();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new("tenant_id", user.TenantId.ToString()),
            new("email", user.Email),
        };
        claims.AddRange(permissions.Select(code => new Claim("perm", code)));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, "email", "perm");
        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync();
        return new ClaimsPrincipal(identity);
    }
}
```

- [ ] **Step 4: Đăng ký DI trong Program.cs**

Sau `builder.Services.AddScoped<IAuthService, AuthService>();` (dòng 73) thêm:
```csharp
builder.Services.AddScoped<ICookieAuthService, CookieAuthService>();
```

- [ ] **Step 5: Chạy test — kỳ vọng PASS**

```bash
powershell -Command "Get-Process dotnet,TourKit.Api -ErrorAction SilentlyContinue | Stop-Process -Force"
dotnet test tests/TourKit.Tests --filter CookieAuthServiceTests -v minimal
```
Kỳ vọng: PASS.

- [ ] **Step 6: Commit**

```bash
git add src/TourKit.Api/Auth/ICookieAuthService.cs src/TourKit.Api/Auth/CookieAuthService.cs src/TourKit.Api/Program.cs tests/TourKit.Tests/Auth/CookieAuthServiceTests.cs
git commit -m "feat(auth): CookieAuthService — xác thực + dựng ClaimsPrincipal (sub/tenant_id/perm)"
```

---

### Task 5: Policy scheme (JWT cho /api, Cookie cho trang) + cấu hình cookie

**Files:**
- Modify: `src/TourKit.Api/Program.cs`
- Test: `tests/TourKit.Tests/Auth/AuthEndpointTests.cs` (chạy lại — không được hồi quy)

**Interfaces:**
- Produces: pipeline auth chọn scheme theo header `Authorization: Bearer` → JWT, ngược lại → Cookie. `/api/v1/*` với bearer vẫn 200; không bearer → thử cookie.

> Chạy `gitnexus_impact({target: "Program"})` trước khi sửa; đây là thay đổi nhạy cảm (ảnh hưởng toàn bộ auth).

- [ ] **Step 1: Sửa khối AddAuthentication**

Thay khối `builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(...)` (dòng 138–154) thành:
```csharp
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "smart";
        options.DefaultChallengeScheme = "smart";
    })
    .AddPolicyScheme("smart", "smart", options =>
    {
        // API gửi Bearer → JWT; trang HTML (không Bearer) → Cookie.
        options.ForwardDefaultSelector = ctx =>
        {
            string? auth = ctx.Request.Headers.Authorization;
            return auth?.StartsWith("Bearer ", StringComparison.Ordinal) == true
                ? JwtBearerDefaults.AuthenticationScheme
                : Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme;
        };
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
            ValidateLifetime = true,
        };
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.Name = "tourkit_auth";
    });
```

- [ ] **Step 2: Chạy lại test auth API — không hồi quy**

```bash
powershell -Command "Get-Process dotnet,TourKit.Api -ErrorAction SilentlyContinue | Stop-Process -Force"
dotnet test tests/TourKit.Tests --filter AuthEndpointTests -v minimal
```
Kỳ vọng: PASS cả 3 test (login/wrong-pass/isolation) — JWT vẫn hoạt động qua policy scheme.

- [ ] **Step 3: Commit**

```bash
git add src/TourKit.Api/Program.cs
git commit -m "feat(auth): policy scheme — JWT cho /api, Cookie cho trang HTML"
```

---

### Task 6: Trang Login / Logout + Dashboard + AuthorizeFolder

**Files:**
- Create: `src/TourKit.Api/Pages/Auth/Login.cshtml`
- Create: `src/TourKit.Api/Pages/Auth/Login.cshtml.cs`
- Create: `src/TourKit.Api/Pages/Auth/Logout.cshtml.cs`
- Create: `src/TourKit.Api/Pages/Dashboard/Index.cshtml`
- Create: `src/TourKit.Api/Pages/Dashboard/Index.cshtml.cs`
- Create: `src/TourKit.Api/Pages/Index.cshtml` + `.cs` (redirect `/` → `/Dashboard`)
- Modify: `src/TourKit.Api/Program.cs` (AuthorizeFolder + cho phép `/Auth` ẩn danh)

**Interfaces:**
- Consumes: `ICookieAuthService.AuthenticateAsync` (Task 4).
- Produces: đăng nhập cookie hoạt động; mọi trang (trừ `/Auth`, `/Ping`) yêu cầu đăng nhập.

- [ ] **Step 1: Login PageModel**

`src/TourKit.Api/Pages/Auth/Login.cshtml.cs`:
```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Auth;

namespace TourKit.Api.Pages.Auth;

[AllowAnonymous]
public class LoginModel : PageModel
{
    private readonly ICookieAuthService _auth;
    public LoginModel(ICookieAuthService auth) => _auth = auth;

    [BindProperty] public InputModel Input { get; set; } = new();
    public string? Error { get; set; }

    public sealed class InputModel
    {
        [Required] public string TenantSlug { get; set; } = "demo-tour";
        [Required, EmailAddress] public string Email { get; set; } = "";
        [Required] public string Password { get; set; } = "";
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        if (!ModelState.IsValid) return Page();

        var principal = await _auth.AuthenticateAsync(Input.TenantSlug, Input.Email, Input.Password);
        if (principal is null)
        {
            Error = "Sai tài khoản, mật khẩu hoặc mã doanh nghiệp.";
            return Page();
        }

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        return LocalRedirect(returnUrl ?? "/Dashboard");
    }
}
```

- [ ] **Step 2: Login view (dùng layout blank)**

`src/TourKit.Api/Pages/Auth/Login.cshtml`:
```cshtml
@page
@model TourKit.Api.Pages.Auth.LoginModel
@{
    Layout = null;
}
<!DOCTYPE html>
<html lang="vi" class="light-style" data-theme="theme-purple">
<head>
    <meta charset="utf-8" /><meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Đăng nhập · TourKit</title>
    <link rel="stylesheet" href="~/vendor/css/core.css" />
    <link rel="stylesheet" href="~/vendor/css/theme-default.css" />
    <link rel="stylesheet" href="~/css/demo.css" />
    <link rel="stylesheet" href="~/css/tourkit.css" />
</head>
<body>
<div class="container-xxl">
  <div class="authentication-wrapper authentication-basic container-p-y">
    <div class="authentication-inner">
      <div class="card">
        <div class="card-body">
          <h4 class="mb-2 fw-bold">TourKit</h4>
          <p class="mb-4">Đăng nhập để tiếp tục</p>
          @if (Model.Error != null)
          {
              <div class="alert alert-danger">@Model.Error</div>
          }
          <form method="post">
            <div class="mb-3">
              <label class="form-label">Mã doanh nghiệp</label>
              <input asp-for="Input.TenantSlug" class="form-control" />
            </div>
            <div class="mb-3">
              <label class="form-label">Email</label>
              <input asp-for="Input.Email" class="form-control" />
            </div>
            <div class="mb-3">
              <label class="form-label">Mật khẩu</label>
              <input asp-for="Input.Password" type="password" class="form-control" />
            </div>
            <button class="btn btn-primary d-grid w-100" type="submit">Đăng nhập</button>
          </form>
        </div>
      </div>
    </div>
  </div>
</div>
</body>
</html>
```

- [ ] **Step 3: Logout + Index redirect + Dashboard**

`src/TourKit.Api/Pages/Auth/Logout.cshtml.cs`:
```csharp
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TourKit.Api.Pages.Auth;

public class LogoutModel : PageModel
{
    public async Task<IActionResult> OnPostAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToPage("/Auth/Login");
    }
}
```

`src/TourKit.Api/Pages/Index.cshtml`:
```cshtml
@page
@model TourKit.Api.Pages.IndexModel
@{ Layout = null; }
```
`src/TourKit.Api/Pages/Index.cshtml.cs`:
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TourKit.Api.Pages;

public class IndexModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Dashboard/Index");
}
```

`src/TourKit.Api/Pages/Dashboard/Index.cshtml.cs`:
```csharp
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TourKit.Api.Pages.Dashboard;

public class IndexModel : PageModel
{
    public void OnGet() { }
}
```
`src/TourKit.Api/Pages/Dashboard/Index.cshtml`:
```cshtml
@page
@model TourKit.Api.Pages.Dashboard.IndexModel
@{ ViewData["Title"] = "Tổng quan"; }
<div class="row">
  <div class="col-12">
    <div class="card">
      <div class="card-body">
        <h5 class="card-title">Chào mừng tới TourKit</h5>
        <p class="mb-0">Chọn <a asp-page="/Customers/Index">Data khách hàng</a> để bắt đầu.</p>
      </div>
    </div>
  </div>
</div>
```

- [ ] **Step 4: AuthorizeFolder trong Program.cs**

Đổi `builder.Services.AddRazorPages();` (thêm ở Task 1) thành:
```csharp
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/");            // mọi trang cần đăng nhập
    options.Conventions.AllowAnonymousToFolder("/Auth"); // trừ đăng nhập/đăng xuất
    options.Conventions.AllowAnonymousToPage("/Ping");   // trang smoke
});
```

- [ ] **Step 5: Kiểm thử thủ công (chạy app)**

```bash
powershell -Command "Get-Process dotnet,TourKit.Api -ErrorAction SilentlyContinue | Stop-Process -Force"
dotnet run --project src/TourKit.Api
```
Mở `http://localhost:5075/` → tự chuyển `/Auth/Login`. Đăng nhập `demo-tour` / `admin@demo.vn` / `Demo@12345`
→ vào `/Dashboard`, thấy menu 19 nhóm bên trái. Bấm Đăng xuất → về Login. Dừng app (Ctrl+C).

- [ ] **Step 6: Commit**

```bash
git add src/TourKit.Api/Pages/Auth src/TourKit.Api/Pages/Dashboard src/TourKit.Api/Pages/Index.cshtml src/TourKit.Api/Pages/Index.cshtml.cs src/TourKit.Api/Program.cs
git commit -m "feat(web): đăng nhập/đăng xuất cookie + dashboard + bắt buộc auth mọi trang"
```

---

### Task 7: Customers — danh sách + lọc + phân trang + thẻ thống kê

**Files:**
- Create: `src/TourKit.Api/Pages/Customers/Index.cshtml`
- Create: `src/TourKit.Api/Pages/Customers/Index.cshtml.cs`

**Interfaces:**
- Consumes: `ICustomerService` (`ListAsync(page,size,filter)`, `GetStatsAsync()`, `GetFilterOptionsAsync()`), DTO `CustomerDto`, `CustomerListFilter`, `CustomerStatsDto`, `CustomerFilterOptionsDto`, `PagedResult<CustomerDto>`.
- Produces: trang `/Customers` (GET) với query `q`, `customerType`, `source`, `city`, `page`.

> Chạy `gitnexus_impact({target: "CustomerService"})` — chỉ đọc, không sửa service; báo là dùng lại nguyên trạng.

- [ ] **Step 1: PageModel danh sách**

`src/TourKit.Api/Pages/Customers/Index.cshtml.cs`:
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Common;
using TourKit.Application.Customers;
using TourKit.Application.Customers.Dtos;

namespace TourKit.Api.Pages.Customers;

public class IndexModel : PageModel
{
    private readonly ICustomerService _service;
    public IndexModel(ICustomerService service) => _service = service;

    [BindProperty(SupportsGet = true)] public string? Q { get; set; }
    [BindProperty(SupportsGet = true)] public int? CustomerType { get; set; }
    [BindProperty(SupportsGet = true)] public string? Source { get; set; }
    [BindProperty(SupportsGet = true)] public string? City { get; set; }
    [BindProperty(SupportsGet = true)] public int Page { get; set; } = 1;

    public const int PageSize = 20;
    public PagedResult<CustomerDto> Result { get; private set; } = new(Array.Empty<CustomerDto>(), 0, 1, PageSize);
    public CustomerStatsDto Stats { get; private set; } = new(0, 0, 0, 0, 0);
    public CustomerFilterOptionsDto Options { get; private set; } =
        new([], [], [], [], [], [], [], [], [], []);

    public async Task OnGetAsync()
    {
        var filter = new CustomerListFilter(Q: Q, CustomerType: CustomerType, Source: Source, City: City);
        Result = await _service.ListAsync(Page < 1 ? 1 : Page, PageSize, filter);
        Stats = await _service.GetStatsAsync();
        Options = await _service.GetFilterOptionsAsync();
    }

    public int TotalPages => (int)Math.Ceiling(Result.Total / (double)PageSize);
}
```

- [ ] **Step 2: View danh sách (bảng Vuexy + thẻ thống kê + thanh lọc)**

`src/TourKit.Api/Pages/Customers/Index.cshtml`:
```cshtml
@page
@model TourKit.Api.Pages.Customers.IndexModel
@{ ViewData["Title"] = "Data khách hàng"; }

<div class="row g-3 mb-4">
  <div class="col-6 col-md">
    <div class="card"><div class="card-body py-3"><small class="text-muted">Tổng khách</small>
      <h4 class="mb-0">@Model.Stats.Total</h4></div></div>
  </div>
  <div class="col-6 col-md">
    <div class="card"><div class="card-body py-3"><small class="text-muted">Mới hôm nay</small>
      <h4 class="mb-0">@Model.Stats.NewToday</h4></div></div>
  </div>
  <div class="col-6 col-md">
    <div class="card"><div class="card-body py-3"><small class="text-muted">Mới tháng này</small>
      <h4 class="mb-0">@Model.Stats.NewThisMonth</h4></div></div>
  </div>
  <div class="col-6 col-md">
    <div class="card"><div class="card-body py-3"><small class="text-muted">Mua lại</small>
      <h4 class="mb-0">@Model.Stats.RepeatBuyers</h4></div></div>
  </div>
</div>

<div class="card">
  <div class="card-header d-flex justify-content-between align-items-center">
    <h5 class="mb-0">Data khách hàng</h5>
    <a asp-page="Create" class="btn btn-primary btn-sm"><i class="bx bx-plus"></i> Thêm khách</a>
  </div>
  <div class="card-body border-bottom">
    <form method="get" class="row g-2">
      <div class="col-md-4"><input name="q" value="@Model.Q" class="form-control" placeholder="Tên / SĐT / email" /></div>
      <div class="col-md-3">
        <select name="source" class="form-select">
          <option value="">— Nguồn —</option>
          @foreach (var s in Model.Options.Sources)
          {
              <option value="@s" selected="@(Model.Source == s)">@s</option>
          }
        </select>
      </div>
      <div class="col-md-3">
        <select name="city" class="form-select">
          <option value="">— Tỉnh/TP —</option>
          @foreach (var c in Model.Options.Cities)
          {
              <option value="@c" selected="@(Model.City == c)">@c</option>
          }
        </select>
      </div>
      <div class="col-md-2"><button class="btn btn-outline-primary w-100">Lọc</button></div>
    </form>
  </div>
  <div class="table-responsive text-nowrap">
    <table class="table table-hover">
      <thead><tr>
        <th>Mã</th><th>Họ tên</th><th>SĐT</th><th>Nguồn</th><th>Tag</th>
        <th class="text-end">Doanh thu</th><th>Ngày tạo</th><th></th>
      </tr></thead>
      <tbody>
      @foreach (var c in Model.Result.Items)
      {
        <tr>
          <td>@c.Code</td>
          <td>@c.FullName</td>
          <td>@c.Phone</td>
          <td>@c.Source</td>
          <td>@c.Tag</td>
          <td class="text-end">@c.Revenue.ToString("#,##0")</td>
          <td>@c.CreatedAt.ToString("dd/MM/yyyy")</td>
          <td class="text-end">
            <a asp-page="Edit" asp-route-id="@c.Id" class="btn btn-icon btn-sm"><i class="bx bx-edit"></i></a>
            <form method="post" asp-page-handler="Delete" asp-route-id="@c.Id" class="d-inline js-delete">
              <button type="submit" class="btn btn-icon btn-sm text-danger"><i class="bx bx-trash"></i></button>
            </form>
          </td>
        </tr>
      }
      @if (Model.Result.Items.Count == 0)
      {
        <tr><td colspan="8" class="text-center text-muted py-4">Không có khách hàng.</td></tr>
      }
      </tbody>
    </table>
  </div>
  <div class="card-footer d-flex justify-content-between align-items-center">
    <small class="text-muted">Tổng @Model.Result.Total khách</small>
    <nav>
      <ul class="pagination pagination-sm mb-0">
        @for (var p = 1; p <= Model.TotalPages; p++)
        {
          <li class="page-item @(p == Model.Page ? "active" : "")">
            <a class="page-link" asp-route-page="@p" asp-route-q="@Model.Q"
               asp-route-source="@Model.Source" asp-route-city="@Model.City">@p</a>
          </li>
        }
      </ul>
    </nav>
  </div>
</div>

@section Scripts {
  <script>
    $(function () {
      $('.js-delete').on('submit', function (e) {
        if (!confirm('Xoá khách hàng này?')) { e.preventDefault(); }
      });
    });
  </script>
}
```
> Handler `Delete` được bổ sung ở Task 10 (cùng `Index.cshtml.cs`). Đợt này nút xoá đã có, handler thêm sau.

- [ ] **Step 3: Build + kiểm thử thủ công**

```bash
powershell -Command "Get-Process dotnet,TourKit.Api -ErrorAction SilentlyContinue | Stop-Process -Force"
dotnet build src/TourKit.Api -v minimal
```
Kỳ vọng: BUILD PASS. (Chạy app, đăng nhập, vào `/Customers` — thấy bảng + thống kê + lọc theo nguồn/tỉnh.)

- [ ] **Step 4: Commit**

```bash
git add src/TourKit.Api/Pages/Customers/Index.cshtml src/TourKit.Api/Pages/Customers/Index.cshtml.cs
git commit -m "feat(web): Customers Index — danh sách + lọc + phân trang + thẻ thống kê"
```

---

### Task 8: Customers — thêm mới (Create + form partial)

**Files:**
- Create: `src/TourKit.Api/Pages/Customers/_Form.cshtml`
- Create: `src/TourKit.Api/Pages/Customers/Create.cshtml`
- Create: `src/TourKit.Api/Pages/Customers/Create.cshtml.cs`

**Interfaces:**
- Consumes: `ICustomerService.CreateAsync(CreateCustomerDto)`.
- Produces: `CustomerFormInput` (view-model dùng chung Create/Edit) với các trường cốt lõi.

- [ ] **Step 1: View-model + form partial**

`src/TourKit.Api/Pages/Customers/_Form.cshtml`:
```cshtml
@model TourKit.Api.Pages.Customers.CustomerFormInput
<div class="row g-3">
  <div class="col-md-6">
    <label class="form-label">Họ tên *</label>
    <input asp-for="FullName" class="form-control" />
    <span asp-validation-for="FullName" class="text-danger"></span>
  </div>
  <div class="col-md-6">
    <label class="form-label">Số điện thoại</label>
    <input asp-for="Phone" class="form-control" />
  </div>
  <div class="col-md-6">
    <label class="form-label">Email</label>
    <input asp-for="Email" class="form-control" />
  </div>
  <div class="col-md-6">
    <label class="form-label">Loại khách</label>
    <select asp-for="CustomerType" class="form-select">
      <option value="0">Khách lẻ</option>
      <option value="1">Khách đoàn</option>
      <option value="2">Đại lý</option>
    </select>
  </div>
  <div class="col-md-6">
    <label class="form-label">Nguồn</label>
    <input asp-for="Source" class="form-control" />
  </div>
  <div class="col-md-6">
    <label class="form-label">Tỉnh/TP</label>
    <input asp-for="City" class="form-control" />
  </div>
  <div class="col-12">
    <label class="form-label">Địa chỉ</label>
    <input asp-for="Address" class="form-control" />
  </div>
</div>
```

`CustomerFormInput` đặt trong `Create.cshtml.cs` (dùng chung, khai báo một lần):
```csharp
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Customers;
using TourKit.Application.Customers.Dtos;

namespace TourKit.Api.Pages.Customers;

public sealed class CustomerFormInput
{
    [Required(ErrorMessage = "Bắt buộc nhập họ tên")]
    public string FullName { get; set; } = "";
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int CustomerType { get; set; }
    public string? Source { get; set; }
    public string? City { get; set; }
    public string? Address { get; set; }
}

public class CreateModel : PageModel
{
    private readonly ICustomerService _service;
    public CreateModel(ICustomerService service) => _service = service;

    [BindProperty] public CustomerFormInput Input { get; set; } = new();

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _service.CreateAsync(new CreateCustomerDto(
            FullName: Input.FullName, Phone: Input.Phone, CustomerType: Input.CustomerType,
            Source: Input.Source, Email: Input.Email, Address: Input.Address, City: Input.City));
        TempData["ok"] = "Đã thêm khách hàng.";
        return RedirectToPage("Index");
    }
}
```

- [ ] **Step 2: Create view**

`src/TourKit.Api/Pages/Customers/Create.cshtml`:
```cshtml
@page
@model TourKit.Api.Pages.Customers.CreateModel
@{ ViewData["Title"] = "Thêm khách hàng"; }
<div class="card">
  <div class="card-header"><h5 class="mb-0">Thêm khách hàng</h5></div>
  <div class="card-body">
    <form method="post">
      <partial name="_Form" model="Model.Input" />
      <div class="mt-4">
        <button type="submit" class="btn btn-primary">Lưu</button>
        <a asp-page="Index" class="btn btn-outline-secondary">Huỷ</a>
      </div>
    </form>
  </div>
</div>
@section Scripts { <partial name="_ValidationScriptsPartial" /> }
```
> Nếu `_ValidationScriptsPartial` chưa có, tạo `src/TourKit.Api/Pages/Shared/_ValidationScriptsPartial.cshtml`:
```cshtml
<script src="~/vendor/libs/jquery/jquery.js"></script>
<script src="~/js/jquery.validate.min.js"></script>
<script src="~/js/jquery.validate.unobtrusive.min.js"></script>
```
(Chỉ thêm nếu file jquery.validate có trong wwwroot; nếu không, bỏ section Scripts — validation server-side vẫn chạy.)

- [ ] **Step 3: Build + kiểm thử thủ công**

```bash
powershell -Command "Get-Process dotnet,TourKit.Api -ErrorAction SilentlyContinue | Stop-Process -Force"
dotnet build src/TourKit.Api -v minimal
```
Kỳ vọng: BUILD PASS. (Chạy app → `/Customers/Create` → nhập tên → Lưu → quay lại danh sách thấy khách mới.)

- [ ] **Step 4: Commit**

```bash
git add src/TourKit.Api/Pages/Customers/Create.cshtml src/TourKit.Api/Pages/Customers/Create.cshtml.cs src/TourKit.Api/Pages/Customers/_Form.cshtml
git commit -m "feat(web): Customers Create — form thêm khách (view-model dùng chung)"
```

---

### Task 9: Customers — sửa (Edit)

**Files:**
- Create: `src/TourKit.Api/Pages/Customers/Edit.cshtml`
- Create: `src/TourKit.Api/Pages/Customers/Edit.cshtml.cs`

**Interfaces:**
- Consumes: `ICustomerService.GetAsync(Guid)`, `UpdateAsync(Guid, UpdateCustomerDto)`, `CustomerFormInput` (Task 8).

- [ ] **Step 1: Edit PageModel**

`src/TourKit.Api/Pages/Customers/Edit.cshtml.cs`:
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Customers;
using TourKit.Application.Customers.Dtos;

namespace TourKit.Api.Pages.Customers;

public class EditModel : PageModel
{
    private readonly ICustomerService _service;
    public EditModel(ICustomerService service) => _service = service;

    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty] public CustomerFormInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var c = await _service.GetAsync(Id);
        Input = new CustomerFormInput
        {
            FullName = c.FullName, Phone = c.Phone, Email = c.Email,
            CustomerType = c.CustomerType, Source = c.Source, City = c.City, Address = c.Address,
        };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid) return Page();
        await _service.UpdateAsync(Id, new UpdateCustomerDto(
            FullName: Input.FullName, Phone: Input.Phone, CustomerType: Input.CustomerType,
            Source: Input.Source, Email: Input.Email, Address: Input.Address, City: Input.City));
        TempData["ok"] = "Đã cập nhật khách hàng.";
        return RedirectToPage("Index");
    }
}
```

- [ ] **Step 2: Edit view**

`src/TourKit.Api/Pages/Customers/Edit.cshtml`:
```cshtml
@page "{id:guid}"
@model TourKit.Api.Pages.Customers.EditModel
@{ ViewData["Title"] = "Sửa khách hàng"; }
<div class="card">
  <div class="card-header"><h5 class="mb-0">Sửa khách hàng</h5></div>
  <div class="card-body">
    <form method="post">
      <partial name="_Form" model="Model.Input" />
      <div class="mt-4">
        <button type="submit" class="btn btn-primary">Cập nhật</button>
        <a asp-page="Index" class="btn btn-outline-secondary">Huỷ</a>
      </div>
    </form>
  </div>
</div>
```
> Route `asp-route-id` từ Index sẽ khớp `@page "{id:guid}"`.

- [ ] **Step 3: Build + kiểm thử thủ công**

```bash
powershell -Command "Get-Process dotnet,TourKit.Api -ErrorAction SilentlyContinue | Stop-Process -Force"
dotnet build src/TourKit.Api -v minimal
```
Kỳ vọng: BUILD PASS. (Chạy app → bấm sửa một khách → đổi tên → Cập nhật → thấy thay đổi.)

- [ ] **Step 4: Commit**

```bash
git add src/TourKit.Api/Pages/Customers/Edit.cshtml src/TourKit.Api/Pages/Customers/Edit.cshtml.cs
git commit -m "feat(web): Customers Edit — sửa khách (dùng chung _Form)"
```

---

### Task 10: Customers — xoá (handler Delete)

**Files:**
- Modify: `src/TourKit.Api/Pages/Customers/Index.cshtml.cs` (thêm `OnPostDeleteAsync`)

**Interfaces:**
- Consumes: `ICustomerService.DeleteAsync(Guid)`.
- Produces: handler `Delete` (khớp `asp-page-handler="Delete"` ở Index Task 7).

- [ ] **Step 1: Thêm handler xoá vào IndexModel**

Trong `src/TourKit.Api/Pages/Customers/Index.cshtml.cs`, thêm phương thức vào class `IndexModel`:
```csharp
    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _service.DeleteAsync(id);
        TempData["ok"] = "Đã xoá khách hàng.";
        return RedirectToPage(new { Page, Q, Source, City });
    }
```

- [ ] **Step 2: Hiện toast TempData ở layout**

Trong `src/TourKit.Api/Pages/Shared/_Layout.cshtml`, ngay sau `<div class="container-xxl flex-grow-1 container-p-y">` thêm:
```cshtml
@if (TempData["ok"] is string okMsg)
{
    <div class="alert alert-success alert-dismissible" role="alert">
        @okMsg
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    </div>
}
```

- [ ] **Step 3: Build + kiểm thử thủ công**

```bash
powershell -Command "Get-Process dotnet,TourKit.Api -ErrorAction SilentlyContinue | Stop-Process -Force"
dotnet build src/TourKit.Api -v minimal
```
Kỳ vọng: BUILD PASS. (Chạy app → xoá một khách → confirm → khách biến mất + toast xanh.)

- [ ] **Step 4: Commit**

```bash
git add src/TourKit.Api/Pages/Customers/Index.cshtml.cs src/TourKit.Api/Pages/Shared/_Layout.cshtml
git commit -m "feat(web): Customers Delete + toast thông báo TempData"
```

---

### Task 11: Chốt phân quyền trang + smoke test tổng + soát lại toàn slice

**Files:**
- Modify: `src/TourKit.Api/Pages/Customers/Index.cshtml.cs`, `Create.cshtml.cs`, `Edit.cshtml.cs` (thêm `[Authorize(Policy = "customer.view")]`)
- Test: `tests/TourKit.Tests/Web/RazorPagesSmokeTests.cs` (thêm case redirect)

**Interfaces:**
- Consumes: policy `"customer.view"` (đã đăng ký sẵn trong `AddAuthorization`).

- [ ] **Step 1: Gắn policy quyền lên các PageModel Customers**

Thêm attribute lên đầu 3 class (`IndexModel`, `CreateModel`, `EditModel`):
```csharp
[Microsoft.AspNetCore.Authorization.Authorize(Policy = "customer.view")]
```

- [ ] **Step 2: Thêm test — trang cần đăng nhập thì redirect**

Thêm vào `tests/TourKit.Tests/Web/RazorPagesSmokeTests.cs`:
```csharp
    [Fact]
    public async Task Customers_page_redirects_to_login_when_anonymous()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        var res = await client.GetAsync("/Customers");
        Assert.Equal(HttpStatusCode.Found, res.StatusCode); // 302 → /Auth/Login
        Assert.Contains("/Auth/Login", res.Headers.Location?.OriginalString ?? "");
    }
```

- [ ] **Step 3: Chạy toàn bộ test**

```bash
powershell -Command "Get-Process dotnet,TourKit.Api -ErrorAction SilentlyContinue | Stop-Process -Force"
dotnet test tests/TourKit.Tests -v minimal
```
Kỳ vọng: PASS toàn bộ (không hồi quy 582 test cũ + test mới).

- [ ] **Step 4: Kiểm tra phạm vi thay đổi trước commit**

```bash
gitnexus_detect_changes({scope: "staged"})
```
Xác nhận chỉ chạm `TourKit.Api` (Pages/Program/Auth/wwwroot) + test.

- [ ] **Step 5: Commit + re-index**

```bash
git add -A
git commit -m "feat(web): phân quyền trang Customers (customer.view) + smoke test redirect"
npx gitnexus analyze
```

---

## Self-Review (đã soát khi viết)

**Spec coverage:**
- Spec §2 (1 deployable, in-process, cookie+JWT, Vuexy vào Api) → Task 1,2,5.
- Spec §3 (cấu trúc thư mục) → Task 1–10.
- Spec §4 (copy Vuexy chọn lọc) → Task 2.
- Spec §5 (auth cookie tái dùng infra) → Task 4,5,6.
- Spec §6 (menu 19 nhóm) → Task 3.
- Spec §7 (module Khách hàng CRUD) → Task 7,8,9,10.
- Spec §8 (kiểm thử) → test ở Task 1,4,5,11 + kiểm thử thủ công mỗi task.
- Spec §9 (ngoài phạm vi: gỡ web/, nhân bản) → không có task (đúng, đợt sau).

**Type consistency:** `CustomerFormInput` khai báo một lần (Task 8), dùng lại Task 9; `MenuNode`/`MenuData` (Task 3) khớp `_VerticalMenu`; `ICookieAuthService.AuthenticateAsync` chữ ký nhất quán Task 4→6; handler `Delete`/`OnPostDeleteAsync` khớp `asp-page-handler="Delete"` (Task 7↔10).

**Placeholder scan:** Menu leaf trỏ `"#"` là **chủ ý** (đợt 1 chỉ /Customers có trang thật) — đã ghi rõ, không phải placeholder kế hoạch. Không có TODO/TBD trong bước thực thi.

**Lưu ý phụ thuộc thứ tự:** nút Delete xuất hiện ở Task 7 nhưng handler ở Task 10 → giữa 7–9 bấm xoá sẽ 400; chấp nhận trong lúc phát triển, hoàn tất ở Task 10.
