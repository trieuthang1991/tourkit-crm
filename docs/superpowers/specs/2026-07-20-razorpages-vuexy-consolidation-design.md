# Gộp UI về ASP.NET Core Razor Pages (Vuexy) — Thiết kế

**Ngày:** 2026-07-20
**Trạng thái:** Đã duyệt hướng, chờ review spec
**Phạm vi đợt 1:** Vertical slice — Khung + Auth + Module Khách hàng

## 1. Bối cảnh & Mục tiêu

Hiện tại UI là một SPA React riêng trong `web/` (Vite + antd + react-query + react-router),
gọi backend `TourKit.Api` qua REST (JWT bearer, 75 controller, kiến trúc phân tầng
Application/Infrastructure/Shared). Việc này buộc **deploy 2 lần** (SPA + API) và phải
đồng bộ token/CORS giữa hai app.

**Mục tiêu:** Bỏ frontend React, chuyển UI sang **Razor Pages dùng bộ component Vuexy**
(HTML/CSS/jQuery/Bootstrap — có sẵn trong `aspnet-core-vue.zip`, thư mục `AspnetCoreFull`),
**gộp thẳng vào `TourKit.Api`** để chỉ còn **một app / một lần publish**. Giữ nguyên toàn bộ
business logic ở tầng Application/Infrastructure.

Đợt 1 chỉ làm **một lát cắt dọc** để chứng minh toàn bộ luồng chạy thật trước khi nhân bản:
khung layout + menu, đăng nhập/đăng xuất cookie, và **module Khách hàng** (Data khách hàng)
làm khuôn mẫu CRUD cho các module sau.

## 2. Quyết định kiến trúc (đã chốt)

| Quyết định | Chọn | Lý do |
|---|---|---|
| Nơi đặt Razor Pages | **Gộp vào `TourKit.Api`** (thêm `AddRazorPages()` + thư mục `Pages/`) | Một deployable, một lần publish |
| Cách lấy dữ liệu | **In-process** — PageModel inject thẳng service Application (`ICustomerService`…) | Không qua HTTP/JWT, nhanh, ít lỗi |
| Auth | **Hai scheme song song**: Cookie (mặc định, cho trang HTML) + JWT (giữ cho `/api/*`) | Trang HTML dùng cookie; API cũ vẫn chạy; cùng đọc claim `sub`/`tenant_id` |
| Nền UI | Bootstrap 5 + jQuery + assets Vuexy (theme tím đã dùng) | Bộ component có sẵn, không dựng framework FE |
| `web/` React | **Giữ nguyên đợt 1**, gỡ ở đợt sau khi UI mới đủ màn | Không phá vỡ luồng đang chạy |

### 2.1 Vì sao cookie tái dùng được toàn bộ hạ tầng

Tenancy & current-user hiện đọc **claim từ `HttpContext.User`**, không phụ thuộc scheme:

- `CurrentUser` (`src/TourKit.Api/Auth/CurrentUser.cs`) đọc claim `sub` → `UserId`.
- `TenantResolutionMiddleware` (`src/TourKit.Api/Tenancy/`) đọc claim `tenant_id` → `AmbientTenantContext`.
- Service Application chỉ phụ thuộc abstraction `ITenantContext` / `ICurrentUserContext` (tầng Shared).

⇒ Chỉ cần cookie mang đúng claim `sub`, `tenant_id` (và các claim quyền) thì **toàn bộ query
filter theo tenant, RBAC, interceptor ghi audit… chạy y nguyên**, không sửa tầng dưới.

## 3. Cấu trúc thư mục (trong `TourKit.Api`)

```
src/TourKit.Api/
  Controllers/            # REST cũ — GIỮ NGUYÊN (/api/v1/*)
  Pages/                  # MỚI — Razor Pages (Vuexy)
    _ViewImports.cshtml
    _ViewStart.cshtml
    Index.cshtml          # điều hướng về /Dashboard hoặc /Auth/Login
    Auth/
      Login.cshtml(.cs)   # đăng nhập cookie
      Logout.cshtml.cs
    Dashboard/
      Index.cshtml(.cs)   # dashboard tối giản (đầu đợt: chào mừng + KPI cơ bản)
    Customers/            # MODULE MẪU
      Index.cshtml(.cs)   # danh sách + lọc + phân trang + thẻ thống kê
      Create.cshtml(.cs)
      Edit.cshtml(.cs)
      _Form.cshtml        # partial dùng chung Create/Edit
      Delete.cshtml.cs    # handler xoá (POST)
    Shared/
      _Layout.cshtml      # layout chính (từ Vuexy _ContentNavbarLayout)
      _VerticalMenu.cshtml # menu 19 nhóm (tái tạo từ AppShell MENU)
      _Navbar.cshtml, _Footer.cshtml
  wwwroot/                # MỚI — assets Vuexy (css/js/fonts/img), Bootstrap, jQuery
  Auth/
    CookieAuthService.cs  # MỚI — xác thực + phát cookie (tái dùng AuthService logic)
  Program.cs              # + AddRazorPages, + Cookie scheme, + MapRazorPages
```

**Nguyên tắc gộp gọn:** route không đụng nhau (`/api/v1/*` cho REST vs đường dẫn trang cho HTML);
`Controllers/` và `Pages/` tách bạch; assets Vuexy nằm trong `wwwroot/`.

## 4. Đưa Vuexy vào (copy có chọn lọc)

Từ `aspnet-core-vue.zip → aspnet-core/AspnetCoreFull/`:

- **Giữ & copy:** `wwwroot/**` (css/js/fonts/img/scss của Vuexy + Bootstrap + jQuery + libs),
  các layout partial ở `Pages/Layouts/**` (đặc biệt `_ContentNavbarLayout`,
  `Sections/Menu/_VerticalMenu`, `Sections/Navbar/_Navbar`, `Sections/Footer/_Footer`),
  `_ViewImports.cshtml`, `_ViewStart.cshtml`. Nếu cần build SCSS: `Gulpfile.js`, `build-config.js`, `package.json`.
- **Bỏ:** toàn bộ trang demo (`Cards`, `Charts`, `Forms`, `FormLayouts`, `ExtendedUi`,
  `LayoutExamples`, `Tables`, `Apps/*` mẫu, `Auth/*` mẫu Vuexy…), `.vs/`, `bin/`, `obj/`,
  `node_modules/`, `.DS_Store`, `AspnetCoreFull.sln/.csproj` (không dùng — ta gộp vào Api).
- **Áp theme tím** đã dùng (memory `vuexy-theme-adoption`): swap biến màu, giữ nhận diện.

## 5. Auth cookie — thiết kế

`CookieAuthService` (mới), tái dùng logic đã có trong `AuthService`:

1. Tra tenant theo slug + user theo email (IgnoreQueryFilters, như `LoginAsync` hiện tại).
2. `IPasswordHasher.Verify` mật khẩu.
3. `LoadPermissionsAsync(userId)` → danh sách permission code (RBAC).
4. Dựng `ClaimsPrincipal` với: `sub`=userId, `tenant_id`=tenantId, `email`, và mỗi permission là
   một claim `perm` (hoặc gộp) → `HttpContext.SignInAsync(CookieScheme, principal)`.
5. Logout: `SignOutAsync`.

**Program.cs bổ sung:**
- `AddAuthentication` giữ JWT nhưng **DefaultScheme = Cookie**; thêm `.AddCookie(...)`
  (LoginPath `/Auth/Login`, cookie HttpOnly, sliding expiration).
- Controller `/api/*` gắn `[Authorize(AuthenticationSchemes = JwtBearerDefaults...)]` để vẫn dùng JWT.
- `TenantResolutionMiddleware` giữ nguyên (đọc claim `tenant_id`, hoạt động với cả cookie).
- `AddRazorPages(options => options.Conventions.AuthorizeFolder("/"))` trừ folder `/Auth`.

**Phân quyền trang:** helper đọc claim `perm` để ẩn/hiện menu item và chặn truy cập trang
(tương đương `filterByPerm` + `perm` trong menu React).

## 6. Menu (tái tạo đúng hệ cũ)

Nguồn chuẩn: hằng `MENU` trong `web/src/app/AppShell.tsx` — **19 nhóm**, submenu lồng, mỗi leaf
có `label` / `to` (route) / `perm`. `_VerticalMenu.cshtml` phải tái tạo **đúng nhãn, thứ tự,
cấu trúc lồng, permission** (theo memory `dont-restructure-menu` — KHÔNG tự chế cấu trúc).

Đợt 1 chỉ có trang thật cho **Data khách hàng** (`/Customers`) + Dashboard + Login; các leaf còn
lại render đúng vị trí nhưng trỏ tạm (placeholder `#` hoặc trang "đang xây dựng") — sẽ nối dần
ở các đợt sau. Active state + mở submenu theo route hiện tại.

## 7. Module mẫu — Khách hàng

Service: `ICustomerService` (`src/TourKit.Application/Customers/`) — inject thẳng vào PageModel.
DTO đã có: `CustomerDto`, `CreateCustomerDto`, `UpdateCustomerDto`, `CustomerListFilter`,
`CustomerStatsDto`, `CustomerFilterOptionsDto`, `PagedResult<CustomerDto>`.

| Trang | Handler | Hành vi |
|---|---|---|
| `Customers/Index` | `OnGetAsync(page, size, filter)` | `ListAsync` (phân trang) + `GetStatsAsync` (thẻ) + `GetFilterOptionsAsync` (dropdown lọc). Bảng Vuewy: cột Mã/Tên/SĐT/Loại/Nguồn/Tag/Doanh thu/Ngày tạo. Thanh lọc + ô tìm `Q`. |
| `Customers/Create` | `OnGet` / `OnPostAsync(CreateCustomerDto)` | Form `_Form.cshtml`, validate (server-side + jQuery validate), `CreateAsync`, redirect Index + toast. |
| `Customers/Edit?id=` | `OnGetAsync(id)` / `OnPostAsync(id, UpdateCustomerDto)` | Nạp `GetAsync`, sửa, `UpdateAsync`. |
| `Customers/Delete` | `OnPostAsync(id)` | `DeleteAsync`, confirm bằng SweetAlert2 (có sẵn Vuexy). |

**Không** làm trong đợt 1 (giữ nhỏ): funnel/dedup/export/care-buckets, chọn tag/segment/assignee
nâng cao. Chỉ các trường cốt lõi của `Create/UpdateCustomerDto` + vài filter chính (Q, CustomerType,
Source, City). Ghi rõ phần hoãn để đợt sau bổ sung.

## 8. Kiểm thử

- **Build:** `dotnet build TourKit.sln` PASS (nhớ kill process giữ DLL trước — memory `kill-api-before-dotnet-test`).
- **Chạy thật:** login bằng tài khoản demo (tạo qua `/registration`, memory `db-switch-sqlite-to-postgres`),
  vào `/Customers`, tạo/sửa/xoá một khách, xác nhận dữ liệu đúng tenant.
- **Unit/integration:** thêm test cho `CookieAuthService` (verify + claims). Không hồi quy 582 test hiện có.
- Xác minh menu render đủ 19 nhóm, phân quyền ẩn/hiện đúng.

## 9. Ngoài phạm vi đợt 1 (các đợt sau)

- Gỡ `web/` React + CORS/SPA config trong `Program.cs`.
- Nhân bản pattern Khách hàng sang các module còn lại (theo thứ tự menu / ưu tiên nghiệp vụ).
- Các màn phức tạp: báo giá calculator, quỹ phòng, lịch điều HDV/xe (Gantt), báo cáo (memory
  `legacy-nghiepvu-4-module`).
- Đầy đủ filter/funnel/dedup/export của Khách hàng.

## 10. Rủi ro & lưu ý

- **Trộn REST + Razor trong 1 project:** chấp nhận để có 1 deployable; tách bạch bằng thư mục & route.
- **Hai auth scheme:** phải test kỹ để `/api/*` vẫn JWT còn trang HTML là cookie; không để cookie
  "rơi" vào endpoint API và ngược lại.
- **Assets Vuexy nặng:** chỉ copy phần dùng; kiểm tra `_ViewStart`/layout trỏ đúng đường dẫn `wwwroot`.
- **GitNexus:** chạy `gitnexus_impact` trước khi sửa `Program.cs`, `AuthService`, và chạy
  `gitnexus_detect_changes` trước commit (theo CLAUDE.md dự án).
