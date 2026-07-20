# Đợt 2 — Wave 0: Bộ components dùng chung (plan chi tiết)

> **For agentic workers:** REQUIRED SUB-SKILL: superpowers:executing-plans. Điều kiện: Đợt 1 xong.

**Goal:** Trích xuất pattern đã verify ở module Khách hàng thành bộ khung dùng lại, để mỗi màn mới (nhất là 27 màn S) chỉ còn khai báo cột + form + service — không copy-paste 150 dòng JS/razor.

**Architecture:** Partial view + `@section` conventions + 1 file JS module chung (`wwwroot/js/tk.js`). KHÔNG dùng ViewComponent phức tạp — giữ đơn giản kiểu template Vuexy. Mọi component tuân `docs/UI-CONVENTIONS.md`.

## Global Constraints
- Refactor Customers sang dùng bộ khung mới NGAY trong đợt này (dogfood — Customers vẫn phải chạy đúng như cũ, verify Chrome).
- 592 test PASS sau mỗi task; không đổi hành vi.

---

### Task 1: `tk.js` — JS module nền (Result/Swal/ajax)

**Files:** Create `src/TourKit.Api/wwwroot/js/tk.js`; Modify `Pages/Shared/Layouts/Sections/_Scripts.cshtml` (nhúng sau menu.js)

**Produces (API JS toàn cục `window.tk`):**
```js
tk.post(url, formData) -> Promise<{isSuccess, message, data}>   // tự gắn antiforgery header
tk.toast(msg)                       // Swal toast góc phải
tk.error(msg)                       // Swal popup lỗi
tk.confirmDelete(opts) -> Promise<bool>   // Swal confirm chuẩn (title/text VN, nút đỏ)
tk.money(n), tk.date(iso)           // format #,##0 và dd/MM/yyyy
```
Steps: viết module (gom code từ `customer-form.js` + Index.cshtml hiện tại) → refactor `customer-form.js` và `Customers/Index.cshtml` dùng `tk.*` → verify Chrome (thêm/sửa/xoá/toast) → commit.

### Task 2: `_TkTable` — khung DataTables server-side

**Files:** Create `Pages/Shared/Components/_TkTable.cshtml` (partial nhận model cấu hình) + hàm `tk.table(selector, opts)` trong tk.js

**Produces:** 1 lời gọi dựng bảng chuẩn: server-side, `autoWidth:false`, ngôn ngữ VN, dom Vuexy, cột `width` bắt buộc, `truncate` helper, hàng action (sửa/xoá) chuẩn. PageModel đi kèm 1 base class:
```csharp
// Pages/Shared/TkListPageModel.cs — helper parse DataTables request + trả JsonResult chuẩn
public abstract class TkListPageModel : PageModel
{
    protected DataTablesRequest ParseDataTables();               // draw/start/length/search + extra filters
    protected JsonResult DataTablesJson(int draw, int total, int filtered, object rows);
}
```
Steps: viết partial + base class → refactor `Customers/Index` dùng khung (giữ nguyên cột/hành vi) → verify search/paging/entries → commit.

### Task 3: `_TkOffcanvas` — khung form thêm/sửa

**Files:** Create `Pages/Shared/Components/_TkOffcanvasShell.cshtml` (vỏ: header + width 640 + form + nút Lưu/Huỷ; body là `RenderSection`-style slot qua `@{ }` content) + `tk.form(formSelector, opts)` trong tk.js

**Produces:** `tk.form` gom: init Select2 (dropdownParent tự tìm offcanvas cha, single + tags), flatpickr (`.tk-date`, altFormat d/m/Y), jQuery Validate (rules truyền qua opts, `ignore:':hidden'`), submit AJAX qua `tk.post` → đóng offcanvas + reload table + toast; `tk.form.open(sel, data|null)` prefill (hỗ trợ select2/multi/flatpickr — logic từ `openCustomerOffcanvas`).
Steps: viết → refactor `_EditOffcanvas.cshtml` + `customer-form.js` (rút còn khai báo field map + rules) → verify: sửa KH doanh nghiệp + tags multi + date → commit.

### Task 4: Lookup selects dùng chung

**Files:** Create `Pages/Shared/Components/_TkLookups.cs` (service nhỏ `ITkLookupService` inject vào PageModel) 

**Produces:**
```csharp
public interface ITkLookupService   // gom các catalog hay dùng, cache per-request
{
    Task<IReadOnlyList<(Guid Id, string Name)>> UsersAsync();
    Task<IReadOnlyList<(Guid Id, string Name)>> BranchesAsync();
    Task<IReadOnlyList<(Guid Id, string Name)>> DepartmentsAsync();
    Task<IReadOnlyList<string>> MarketTypesAsync();
    Task<IReadOnlyList<CustomerTypeDto>> CustomerTypesAsync();
    Task<IReadOnlyList<string>> CustomerSourcesAsync();
    Task<IReadOnlyList<string>> CustomerTagsAsync();
}
```
+ endpoint suggestion khách `GET /Customers?handler=Suggest&q=` (Take 10, tên/SĐT — nền cho S5 và các form Booking sau).
Steps: implement (ưu tiên gọi service Application có sẵn) → thay `LoadCatalogsAsync` của Customers → test + commit.

### Task 5: Khung Detail + khung Print

**Files:** Create `Pages/Shared/Components/_TkDetailShell.cshtml` (2 cột Vuexy user-view: slot card trái / pills+nội dung phải), `Pages/Shared/Layouts/_PrintLayout.cshtml` (layout trắng khổ A4, nút In, không menu/navbar)

Steps: trích từ `Customers/Details.cshtml` → refactor Details dùng shell → verify → commit. (_PrintLayout chưa có consumer — smoke bằng trang Ping-Print tạm hoặc để trống tới Wave 1 Quote print; chọn: viết + test khi in Báo giá, đánh dấu rõ.)

### Task 6: Chart wrapper (chuẩn bị Wave 4, làm mỏng)

`tk.chart(el, apexOptions)` — wrapper ApexCharts (assets Vuexy có sẵn `vendor/libs/apex-charts`) + preset màu theme tím. CHỈ viết wrapper + 1 demo ở Dashboard placeholder (1 card chart doanh thu 7 ngày từ ReportsController nếu endpoint có sẵn, ngược lại dùng số stats Customer). Commit.

### Task 7: Chốt đợt
- Customers chạy đúng như trước (checklist Chrome: list/search/paging/thêm/sửa doanh nghiệp/xoá/detail/toast).
- 592 test PASS; cập nhật `docs/UI-CONVENTIONS.md` mục 8: bắt buộc dùng `tk.js` + `_Tk*` cho màn mới.
- **Định nghĩa xong-Wave-0:** một dev (hoặc subagent) dựng được màn CRUD danh mục mới trong ≤150 dòng tổng (PageModel + cshtml).

**Hoãn có chủ ý:** Gantt/Calendar/Kanban/Allotment shell — viết khi chạm màn đầu tiên dùng nó (Wave 1: Calendar cho Departures; Wave 2: Gantt/Allotment) để tránh thiết kế chay.
