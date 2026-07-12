# TourKit CRM — Tài liệu BÀN GIAO cho AI kế nhiệm

> Mục tiêu dự án: **migrate hệ điều hành tour cũ (KojiCRM, ~127 bảng) sang stack mới**, và
> **bám sát giao diện + nghiệp vụ hệ cũ**. Đây là điểm vào duy nhất cho AI/dev mới. ĐỌC HẾT file này trước.

---

## 0. Đọc trước tiên — 2 nguồn ngữ cảnh

### a) Claude Memory (rules + ngữ cảnh tích luỹ) — `docs/handoff/memory/`
Toàn bộ trí nhớ phiên trước đã copy vào `docs/handoff/memory/`. **Đọc `MEMORY.md` (index) rồi đọc từng file.**
Nếu bạn là Claude Code trên máy này, memory gốc nằm ở
`C:\Users\trieu\.claude\projects\D--MiGroup-AI-tourkit-crm\memory\` và tự nạp vào context mỗi phiên —
khi cập nhật, sửa ở CẢ HAI nơi (hoặc coi `docs/handoff/memory/` là bản chính rồi copy ngược lại).

Các rule CỐT LÕI (chi tiết trong memory):
1. **Nghiệp vụ bám hệ cũ** — không tự chế logic. Nguồn: `script.sql`, staging live, mockup, repo cũ. → `business-logic-follow-old-project.md`
2. **KHÔNG tạo table/schema DB cho dữ liệu tooling** (GitNexus, memory) — giữ file-based. → `no-tables-for-tooling-data.md`
3. **NCC ngoài (SMS/Zalo/Bank/OCR/CRM) → 1 API Gateway chung**, không client rời. Vendor đã chốt (eSMS/ZNS/Casso/FPT.AI), chờ key. → `external-providers-gateway.md`
4. **Mở rộng entity bám hệ cũ = cột JSON cho field mềm/list + ID kiểu STRING (không Guid)** để migrate dữ liệu cũ. → `entity-extend-json-string-pattern.md`
5. **Mỗi màn danh sách phải có: hàng THẺ THỐNG KÊ + thanh SEARCH/LỌC + cột giàu** (bám staging), không chỉ bảng. → `screen-needs-search-and-stats.md`
6. **UI bám staging**: brand đỏ `#EB5324`, sidebar `#333`, font Roboto; menu gom nhóm đúng thứ tự KojiCRM; trang chủ = "Bàn làm việc". → `legacy-ui-reference.md`

### b) GitNexus (code intelligence) — `.gitnexus/` (đã commit trong repo)
Graph đã index sẵn (~7300 symbols). Dùng để hiểu code / đánh giá blast-radius TRƯỚC khi sửa.
- Refresh sau khi commit: `npx gitnexus analyze` (hook PostToolUse tự chạy sau `git commit`/`git merge`).
- MCP tools: `gitnexus_query({query})`, `gitnexus_context({name})`, `gitnexus_impact({target,direction:"upstream"})`, `gitnexus_detect_changes()`.
- Hướng dẫn đầy đủ: `CLAUDE.md` (mục GitNexus) + `.claude/skills/gitnexus/*`.
- **Bắt buộc**: chạy `gitnexus_impact` trước khi sửa symbol; cảnh báo nếu HIGH/CRITICAL.

---

## 1. Chạy dự án (Quick start)

> Đường dẫn thật lồng 1 cấp: **`D:/MiGroup/AI/tourkit-crm/tourkit-crm`**.

**Backend API (.NET 9, SQLite tự migrate+seed khi khởi động):**
```bash
cd src/TourKit.Api
ASPNETCORE_ENVIRONMENT=Development dotnet run --no-launch-profile --urls http://localhost:5075
```
**Web (React+Vite, Ant Design):**
```bash
cd web && npm run dev   # http://localhost:5173  (gọi thẳng API :5075, không proxy)
```
`.claude/launch.json` ở gốc NGOÀI (`D:/MiGroup/AI/tourkit-crm/.claude/`) với `cwd: tourkit-crm/web` (cho preview tool).

**Tài khoản demo (local):** tổ chức `demo-tour` · `admin@demo.vn` · `Demo@12345`.

**Hệ cũ tham chiếu (LIVE):** https://staging.tourkit.vn/ — user `admin` (mật khẩu do CHỦ DỰ ÁN tự nhập;
AI KHÔNG được gõ mật khẩu vào form đăng nhập — quy tắc an toàn). Route hệ cũ: `/customer-data`,
`/customer-debt`, `/payment-vouchers-waiting`, `/customer-care`, `/work-space-view`...
Mockup tĩnh + menu: `D:/MiGroup/tourkitapp/tourkit/UI/*.html`, `.../CMS/KojiCRM/Pages/Controls/MenuLeft.ascx`.

---

## 2. Kiến trúc & quy ước

- **.NET**: Api → Application → Infrastructure → Shared (Clean, RESTful services, KHÔNG CQRS).
  - Auto-DI Scrutor (`I{X}Service`→`{X}Service`), FluentValidation, `ApplyConfigurationsFromAssembly`, generic `IRepository<>`/`Repository<>`.
  - Multi-tenant: `ITenantEntity` + global query filter + SaveChanges interceptor. `ICurrentUserContext.UserId` (Shared.Security) cho user hiện tại.
  - **Warnings-as-errors**: CA1859 (dùng `Dictionary` cụ thể ở tham số, không `IReadOnlyDictionary`), CA1873, CA1031, CA1861.
  - Migration: EF Core; DEV SQLite tự tạo lại. Khi đổi entity → `dotnet ef migrations add <Name> --project src/TourKit.Infrastructure --startup-project src/TourKit.Api`. **Kill API (khoá DLL) trước khi build**: `taskkill //PID <pid on :5075> //F`.
  - **Mở rộng DTO record (positional)**: thêm field MỚI có DEFAULT để không vỡ call site; cập nhật test helper + zod fixtures.
- **Web**: React + TS strict + Vite + Ant Design + TanStack Query + react-hook-form + zodResolver.
  - Mẫu: `ResourcePage`/`makeCrud`/`CrudFormModal`/Field (`TextField/NumberField/SelectField/DatePickerField/TextAreaField/CheckboxField`). `SelectField` có `mode="multiple"|"tags"` + `showSearch`.
  - **Màn phức tạp (có stats+search) → build CUSTOM** (không qua ResourcePage) — xem `CustomersPage.tsx` làm MẪU CHUẨN.
  - Render cột **PHÒNG THỦ** với mảng: `const arr = v => Array.isArray(v)?v:[]` (tránh crash cell).
- **Test**: xUnit (unit: `FakeRepository<T>`; integration: `WebApplicationFactory`/`AuthTestFactory` InMemory). Web: vitest.
  - Hiện tại: **357 unit · 107 integration · 4 arch · 125 web** (xanh).
  - Chạy: `dotnet test tests/TourKit.UnitTests/...`, `.../TourKit.Tests/...`; `cd web && npx tsc --noEmit && npx vitest run`.

---

## 3. Trạng thái hiện tại (git)

- Nhánh làm việc mặc định: **`dev`** (chủ dự án yêu cầu làm trên `dev`, merge về `main` khi chốt mốc).
- `dev` HEAD ~ `56e7eb3` (màn Data khách hàng: model mở rộng + thống kê + search). `main` ~ `2e1bbd8`.
- FF-merge: `git switch main && git merge --ff-only dev`. Xong reindex GitNexus.

### Đã làm phiên này (bám giao diện hệ cũ)
- Menu gom 15 nhóm + đúng thứ tự KojiCRM; brand đỏ #EB5324 + Roboto + sidebar #333.
- Trang chủ **"Bàn làm việc"** (command-center: hồ sơ + donut việc + tạo nhanh + thông báo + công nợ + **lịch tháng khởi hành** inline + bảng "Công việc của tôi").
- Giàu cột: Khách hàng, NCC, Đơn hàng (BE: tên KH/tour/đã thu/còn nợ), Đặt dịch vụ.
- Trang mới: **Phiếu thu, Phiếu chi** (list toàn tenant + duyệt).
- Form Đặt dịch vụ: dropdown thay UUID.
- **Data khách hàng = TEMPLATE HOÀN CHỈNH**: model CRM mở rộng (JSON `CrmProfileJson` + Code + ID string + list segments/tags/assignedTo) + **thẻ thống kê** (`GET /customers/stats`) + **search/lọc** (`?q=&customerType=`) + cột giàu + form đầy đủ. Xem `CustomersPage.tsx`, `CustomerService.cs`, `CustomerCrmProfile.cs`.

---

## 4. VIỆC CÒN LẠI (ưu tiên cho AI kế nhiệm)

1. **Roll-out template Data khách hàng sang các màn khác** (việc chính hiện tại). Mỗi màn:
   - Xem staging màn đó (stats gì / filter gì / cột gì) — vd Đơn hàng `/`, NCC, Phiếu thu/chi, Lead `/customer-data`-tương-tự.
   - Mở rộng model nếu thiếu (theo pattern JSON+string-ID+list).
   - Dựng: hàng thẻ Statistic + Input.Search + Select lọc + cột giàu + render phòng thủ.
   - Chủ dự án định hướng ưu tiên "các màn giao dịch chính trước" (Đơn hàng, NCC, Phiếu thu/chi, Lead) nhưng CHƯA chốt — HỎI LẠI trước khi làm loạt.
2. **Data khách hàng sâu hơn**: bảng con **Danh sách liên hệ** (nhiều người liên hệ/KH), bộ lọc đầu trang (chi nhánh, phễu khách), Xuất/Nhập file.
3. **Module hệ cũ còn thiếu**: Vé Máy Bay, Điều hành Tour (phiếu điều hành DV), HRM (hồ sơ NV: ngày vào làm/thâm niên — trang "Bàn làm việc" đang để trống mấy field này).
4. **API Gateway cho NCC ngoài** (SMS/Zalo/Bank/OCR/CRM) — CHỜ credential + thiết kế gateway (chủ dự án cấp). Seam abstraction đã sẵn (`ISmsSender/IZaloSender/IEmailSender`).
5. **Task nhỏ đã flag**: sửa `web/src/features/finance/receiptTypes.ts` `RECEIPT_STATUS` (đang map 1/2/3 nhưng backend là 0/1/2 — lệch, panel phiếu thu hiện nhãn sai). Chuẩn đúng ở `finance/listTypes.ts` `VOUCHER_STATUS`.

---

## 5. BẪY vận hành (đọc kỹ — tiết kiệm thời gian)

- **Dev-server HMR bị kẹt** sau nhiều lần sửa liên tục → gây **trang trắng** hoặc **lỗi Cell2 console giả** (trỏ file/timestamp CŨ). KHÔNG phải lỗi code. **Cách xử lý: restart Vite dev server** (`preview_stop` + `preview_start`, hoặc kill+`npm run dev`). Xác nhận code OK bằng `cd web && npx vite build` (build production thành công = code đúng).
- **JWT hết hạn ~1h** → đăng nhập lại (mật khẩu demo ở trên; staging thì user tự nhập).
- **Kill API trước khi build backend** (khoá DLL): tìm PID trên `:5075` rồi `taskkill //PID <pid> //F`.
- **GitNexus reindex** churns file nhị phân ~100MB + đổi số symbol trong CLAUDE.md/AGENTS.md → chỉ reindex ở MỐC MERGE, không mỗi commit dev.
- **rtk hook** đôi khi rewrite lệnh bash (grep/find) → dùng công cụ Grep/Glob chuyên dụng thay vì bash grep khi được.
- **File legacy `script.sql` / .ascx là UTF-16** → bash grep ra rỗng; dùng công cụ Grep (ripgrep) hoặc đọc bằng Read.

---

## 6. Bản đồ file quan trọng

- Menu/layout/brand: `web/src/app/AppShell.tsx`, `web/src/app/providers.tsx` (theme token), `web/index.html` (Roboto).
- Template màn có stats+search: `web/src/features/customers/CustomersPage.tsx` + `types.ts` + `customersCrud.ts`.
- Backend Customer: `src/TourKit.Shared/Entities/Customer.cs`, `src/TourKit.Application/Customers/{CustomerService,CustomerCrmProfile,ICustomerService}.cs`, `.../Dtos/CustomerDtos.cs`, `src/TourKit.Api/Controllers/CustomersController.cs`.
- Trang chủ: `web/src/features/workspace/WorkspacePage.tsx` (+ `TaskDonut.tsx`), lịch: `web/src/features/booking/DepartureCalendar.tsx`.
- Phiếu thu/chi: `web/src/features/finance/{ReceiptsListPage,PaymentsListPage,listTypes}.tsx`.
- Gap nghiệp vụ 127 bảng: `docs/business/legacy-feature-gap-analysis.md`.
- Roadmap chi tiết + lịch sử: `docs/handoff/memory/roadmap-status.md`.

---

**TL;DR cho AI mới:** Đọc `docs/handoff/memory/MEMORY.md` + các file memory → nắm rules. Dùng GitNexus để hiểu code.
Chạy API+web theo mục 1, đối chiếu staging.tourkit.vn. Việc chính: **roll-out template `CustomersPage` (stats+search+model)
sang các màn giao dịch** — HỎI chủ dự án ưu tiên màn nào trước. Bám 6 rule cốt lõi. Cẩn thận bẫy HMR (mục 5).
