# TourKit — Bộ quy ước chuẩn UI/UX & Code

> Mục tiêu: sản phẩm **nhất quán, chuyên nghiệp, khách hàng dùng được**. Mọi màn/form MỚI hay SỬA đều PHẢI theo tài liệu này. Ưu tiên **dùng lại component có sẵn của template Vuexy** (đã tích hợp trong `wwwroot/vendor/libs`), chỉ dựng mới khi chưa có.

Stack UI: ASP.NET Core Razor Pages + Bootstrap 5 + jQuery + Vuexy. Icon: **Tabler** (`ti ti-*`). Ngôn ngữ: **Tiếng Việt**, thuật ngữ bám hệ cũ (KojiCRM/staging).

---

## 1. Form (input / select / textarea)

| Quy tắc | Bắt buộc |
|---|---|
| **Placeholder** gợi ý ở mọi input/select/textarea | ✅ (vd "09xxxxxxxx", "Nhập mã số thuế"; select: option "— Chọn … —" hoặc `data-placeholder`) |
| **Dấu sao đỏ** ở label field bắt buộc | ✅ `<label>Họ tên <span class="text-danger">*</span></label>` |
| **Validate bằng jQuery Validate** (client) — KHÔNG dùng `required` HTML5/validation mặc định | ✅ `vendor/libs/jquery-validation`; chặn cả submit AJAX tới khi hợp lệ |
| **Luôn validate lại phía server** (ModelState/FluentValidation) — không tin client | ✅ |
| **Date/giờ dùng flatpickr** — KHÔNG `<input type="date">` | ✅ `vendor/libs/flatpickr`, format `d/m/Y` |
| **Select nhiều option dùng Select2** (search); multi khi cần (Tags) | ✅ `vendor/libs/select2`; trong offcanvas set `dropdownParent` |
| Field auto (mã KH…) để `readonly`/`disabled`, placeholder "Hệ thống tự tạo" | ✅ |
| Rule động (vd Tên đơn vị bắt buộc chỉ khi Loại khách = Doanh nghiệp) phải chạy được | ✅ |
| Label nằm TRÊN field; nhóm 2 cột `col-md-6` cho gọn | ✅ |

## 2. Danh mục (dropdown) — KHÔNG bịa, KHÔNG distinct-từ-data

- Loại khách / Nguồn / Thẻ / Thị trường / Tỉnh thành… = **danh mục cố định hoặc catalog** (bám hệ cũ dùng bảng danh mục). Nạp từ service catalog (`ICustomerTypeService`, `ICustomerSourceService`…) hoặc reference cố định (`VietnamProvinces`).
- Value lưu = tên/mã chuẩn của danh mục để prefill khi sửa khớp option.

## 3. Bảng / danh sách

- Dữ liệu lớn: **DataTables server-side** (`serverSide: true`, handler `OnGetData`). Không tải hết ra client.
- Bắt buộc có: ô **tìm kiếm**, **chọn số dòng/trang**, **"Hiện X–Y trên Z"** — **ngôn ngữ Tiếng Việt** (`language:{...}`).
- **Empty state** rõ ràng ("Chưa có …", "Không tìm thấy …").
- **Định dạng**: tiền `#,##0` (phân cách nghìn), ngày `dd/MM/yyyy`.
- **Fix width cột** tránh vỡ/xô lệch: `autoWidth:false` + `columns[].width` cố định + cắt chuỗi dài (`text-truncate` + `title` tooltip). Không để cột co giãn loạn khi reload.
- Hành động dòng nhất quán: **Sửa → offcanvas**, **kích Mã/Tên → trang chi tiết**, **Xoá → xác nhận SweetAlert2**.

## 4. Thêm / Sửa → Offcanvas (trượt phải)

- Dùng **offcanvas `offcanvas-end`** (không mở trang mới cho thao tác sửa nhanh). Rộng vừa mắt (~640px), responsive `max-width:100%`.
- Lưu **AJAX** (không rời trang) → đóng offcanvas + **toast** + reload bảng. Gửi kèm antiforgery token qua header.
- 1 offcanvas dùng chung Thêm & Sửa (Id rỗng = thêm).

## 5. Trang chi tiết

- Layout **Vuexy user-view**: cột trái **card hồ sơ** (avatar/tên/badge + số liệu nhanh + danh sách thông tin + nút Sửa) · cột phải **nav-pills + nội dung/timeline**.
- Nút **Sửa** ở chi tiết cũng mở offcanvas (dùng lại component).

## 6. Phản hồi & trạng thái

- **KHÔNG dùng `alert()`/`confirm()` mặc định của trình duyệt.** Dùng **SweetAlert2** (`vendor/libs/sweetalert2`): toast góc phải khi thành công, popup lỗi, và **confirm xoá** (nút styled `btn btn-danger`/`btn-label-secondary`, `buttonsStyling:false`).
- **Kết quả AJAX theo format chuẩn** `{ isSuccess, message, data }` — trả bằng class `TourKit.Api.Web.Result` (`Result.Success(msg, data)` / `Result.Error(msg)`; kế thừa `SuccessResult`/`ErrorResult`). KHÔNG trả kiểu tuỳ tiện `{ ok, error }`. JS đọc `res.isSuccess`/`res.message`.
- Bảng DataTables giữ contract riêng (`draw/recordsTotal/recordsFiltered/data`) — không bọc `Result`.
- Toast thành công server-side (redirect) qua `TempData` — lưu ý layout Vuexy gọi `TempData.Keep()` → phải `TempData.Remove()` sau khi đọc để không dính.
- Trạng thái: **loading / empty / error** đều phải có, không để trắng trơn.

## 7. Điều hướng & phân quyền

- Menu bám **đúng hệ cũ** (19 nhóm — xem memory `dont-restructure-menu`), không tự chế cấu trúc.
- **Ẩn/chặn theo quyền** (`perm` claim): item menu và trang đều lọc theo quyền.
- Breadcrumc/tiêu đề rõ trang đang ở đâu.

## 8. Component & tài nguyên

- **Dùng lại** component Vuexy có sẵn: Select2, flatpickr, jQuery Validate, SweetAlert2, DataTables, offcanvas, avatar-initial, timeline, bg-label-*. **Không** reinvent.
- Icon **Tabler** (`ti ti-*`) — KHÔNG boxicons. Màu theo token Vuexy (theme tím `theme-default`).
- Thẻ thống kê: icon-chip màu (`avatar-initial rounded bg-label-*`) — không để card số trơn.

## 9. Định dạng dữ liệu (VN)

- Tiền: `#,##0` VND. Ngày: `dd/MM/yyyy`. SĐT chuẩn hoá. MST/CMND kiểu chuỗi.
- Chuỗi hiển thị Tiếng Việt có dấu, thuật ngữ nhất quán toàn app.

## 10. Chất lượng / an toàn

- **Build 0 warning** (dự án `TreatWarningsAsErrors`). Kill `:5075` trước build/test (memory `kill-api-before-dotnet-test`).
- **Test không hồi quy** trước khi commit. Chạy thật (login demo `demo-tour`/`admin@demo.vn`/`Demo@12345`) verify màn.
- Chạy `gitnexus_impact` trước khi sửa symbol; `gitnexus_detect_changes` trước commit (CLAUDE.md).

---

**Tham chiếu triển khai mẫu:** module Khách hàng (`Pages/Customers/*`) — list DataTables server-side, offcanvas Thêm/Sửa (Select2 + flatpickr + jQuery Validate + section động), trang chi tiết Vuexy. Nhân bản pattern này cho các module khác.
