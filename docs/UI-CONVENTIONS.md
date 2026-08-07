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

## 2b. Search (bắt buộc — bám hệ cũ có bỏ dấu)

- **Text search phải KHÔNG DẤU**: dùng cột `SearchName` (lower + bỏ dấu, set khi Create/Update qua `VietnameseText.NormalizeSearch`) + so `ILIKE/Contains`. KHÔNG search thẳng cột gốc (case/accent-sensitive). Postgres: GIN `gin_trgm_ops`.
- **Search SĐT**: input toàn số (≥4) → khớp cột `PhoneNormalized` (`VietnameseText.NormalizePhone`, +84→0) để gõ `0901` ra cả `+84901`.
- **Phân trang ở SQL** (fast-path `PageAsync`) khi không có filter phải lọc in-memory; tránh kéo cả bảng rồi Skip/Take.
- **Đếm/thống kê ở SQL** (`CountAsync`/GROUP BY qua query riêng) — KHÔNG `ListAsync()` rồi `.Count()`.
- Mẫu: `CustomerService.ListAsync/GetStatsAsync/FindByPhoneAsync`. Áp pattern này cho MỌI service list mới.

## 2. Danh mục (dropdown) — KHÔNG bịa, KHÔNG distinct-từ-data

- Loại khách / Nguồn / Thẻ / Thị trường / Tỉnh thành… = **danh mục cố định hoặc catalog** (bám hệ cũ dùng bảng danh mục). Nạp từ service catalog (`ICustomerTypeService`, `ICustomerSourceService`…) hoặc reference cố định (`VietnamProvinces`).
- Value lưu = tên/mã chuẩn của danh mục để prefill khi sửa khớp option.

## 3. Bảng / danh sách

> **Màn danh sách MỚI dùng `tk.grid` (Tabulator), không dùng DataTables nữa.** DataTables chỉ còn
> ở các màn chưa kịp chuyển. Mẫu chuẩn: `Pages/CustomersTabulator/Index.cshtml` (khách hàng) và
> `Pages/Orders/Index.cshtml` (đơn hàng).

### 3a. `tk.grid` — lưới chuẩn (`wwwroot/js/tk-grid.js` + `css/tk-grid.css`)

- Trang chỉ khai báo **cột dữ liệu** + **menu hành động**; cột ô chọn và cột ⋮ do `tk.grid` tự thêm.
- Gói sẵn: phân trang từ server (`?handler=Data`), thanh tác vụ hàng loạt (`#bulkbar`),
  menu hành động (chuột phải trên dòng **và** nút ⋮), dòng tổng `topCalc` ngay dưới tiêu đề,
  bảng **fit đúng 1 màn** (chỉ một thanh cuộn, nằm trong bảng), và nối sẵn thanh lọc chuẩn:
  `#f-q`, `#btn-search`, `#btn-reset`, `#btn-adv` + `#adv`, `.tk-chip` (chip lọc nhanh),
  `#type-tabs[data-field]` (tab phân loại), `#btn-export`.
- Ô nhiều dòng phải dựng bằng `tk.g.*` (`stack` / `avatar` / `icLine` / `chip` / `moneyCell` / `pick`)
  để mọi màn cùng một kiểu: dòng chính đậm + dòng phụ mờ, tối đa 2 dòng.
- **Ẩn/hiện cột**: có sẵn, không phải làm gì thêm. Nút ⚙ ở tiêu đề cột ⋮ (ngoài cùng phải) mở
  danh sách cột; rê chuột vào một cột thì cột đó cũng có nút riêng để tắt nhanh. Lựa chọn lưu
  ở `localStorage` theo `tk.cols:<đường dẫn><selector>` và **chỉ ghi cột người dùng tự bật/tắt**
  — nếu ghi "mọi cột đang ẩn" thì cột do trang tự ẩn theo loại (vd cột Visa) sẽ mất luôn ở màn
  đáng lẽ phải hiện.
- Handler dữ liệu: `ParseDataTables()` (đã hiểu cả `page/size/q` của tk.grid) + `GridJson(dt, …)`
  ở `TkListPageModel` — payload phục vụ đồng thời Tabulator và DataTables.
- **Năm luật chủ dự án đã chốt** (đã cài trong `tk.grid`, đừng làm khác):
  1. **Kích vào dòng KHÔNG làm gì** — không tích chọn (`selectableRows:'highlight'`), không mở form sửa.
     Mọi hành động đi qua nút ⋮ hoặc chuột phải. (Trước đây kích dòng mở form sửa; chủ dự án đã bỏ
     vì hay bấm nhầm.) Ngoại lệ duy nhất: ô được thiết kế riêng để đổi giá trị — xem luật 4.
  2. Thanh tác vụ hàng loạt **nổi bên trong bảng** (`.tk-bulkbar`), không đẩy nội dung, không tăng chiều cao.
  3. Badge trạng thái/loại dùng formatter chuẩn `tkBadge`/`tkStack`/`tkMedia`; `tkBadge` lấy nhãn từ
     GIÁ TRỊ cột nên cột phải trỏ `statusLabel`, không phải mã số `status`.
  4. Ô **đổi được ngay trên bảng** (trạng thái / loại) dựng bằng `tk.g.pick(label, color, title)` +
     `clickMenu` của cột — nó ra một nút có viền/nền khi rê chuột nên người dùng biết bấm được.
     Hết bước hợp lệ (đơn đã huỷ…) thì trả badge tĩnh, **đừng** vẽ nút bấm vào không ra gì.
  5. Số liệu kiểu Việt (app đặt vi-VN bằng `UseRequestLocalization`) + `tabular-nums` cho cột tiền.
- **Bẫy đã gặp, đừng lặp lại:**
  - Không đặt `selectableRowsRangeMode:'click'` (chỉ chọn được 1 dòng).
  - **Tuyệt đối không** gọi `setHeight` trong `renderComplete` (vòng lặp vô hạn → treo trang).
  - select2 phát sự kiện change kiểu jQuery nên phải bind qua jQuery, không `addEventListener`.
  - Tabulator 6 **bỏ** callback khai báo trong options — `rowClick` phải đăng ký qua `table.on(...)`.
  - **Menu của Tabulator tự ép `height` = chiều cao cả trang** khi dưới điểm bấm không đủ chỗ
    (`Popup._fitToScreen`): menu thành hộp trắng khổng lồ và kéo dài trang. `tk-grid.js/placeMenu`
    gỡ chiều cao đó, chuyển menu sang `position:fixed` rồi lật lên trên; CSS chốt thêm
    `height:auto !important` + `max-height`. Đừng bỏ hai chỗ này.
  - **Hộp thả select2 gắn vào `<body>`** mà khai báo `width:'100%'` thì rộng bằng 100% BODY → tràn
    phải, trang mọc thanh cuộn ngang rồi kéo theo cả cuộn dọc. Đã chặn bằng
    `body > .select2-container { width: auto !important }`.
  - Mở/đóng panel `#adv` làm đỉnh bảng tụt xuống → phải tính lại chiều cao, nếu không trang mọc
    thanh cuộn ngoài. `tk.grid` đã gắn `ResizeObserver` lên các khối anh em của bảng.

### 3c. Khung Kanban (jKanban)

Màn có vòng đời trạng thái rõ ràng thì thêm nút chuyển **Bảng ↔ Kanban** ở góc phải đầu trang
(`#view-table` / `#view-kanban`). Đang dùng ở: Cơ hội bán hàng, Công việc, Quản lý lịch hẹn.

- Thư viện: **jKanban** đi kèm theme (`vendor/libs/jkanban`) + `vendor/css/pages/app-kanban.css`.
  Không mua thư viện Kanban trả phí (Bryntum…) — chủ dự án đã chốt bỏ mọi thứ dính license.
- Mỗi cột nạp **riêng một trang** qua `?handler=KanbanColumn&status=..&page=..&size=15`, kèm nút
  "Tải thêm". Không bao giờ đổ cả bảng ra client.
- Kéo–thả gọi `?handler=Move` → service phải có **`MoveAsync(id, status)` chỉ đổi trạng thái**.
  Đừng đọc bản ghi rồi `UpdateAsync` lại từ trang: giữa hai thao tác người khác sửa gì là mất.
  Server từ chối thì gọi lại `kbRender()` để thẻ về đúng cột — không để màn hình nói dối.
- Dải thống kê + thanh lọc **ở lại** khi chuyển sang Kanban (dùng chung bộ lọc); riêng tiêu chí
  **trạng thái bị loại khỏi truy vấn Kanban** vì mỗi cột đã là một trạng thái.
- Chiều cao khung gò bằng `tk.fitBox('#pane-kanban', 320)` — cuộn trong khung, trang không cuộn.

### 3b. Màn cũ còn dùng DataTables

Chỉ còn màn **Data khách hàng bản cũ** (`/khach-hang/ban-cu`, giữ để đối chiếu) và các **danh mục nhỏ**
dùng `tk.tableClient` (bảng render sẵn ở server). Toàn bộ 35 màn danh sách server-side đã chuyển sang `tk.grid`.

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
- **Đầu và chân đứng yên, chỉ ruột cuộn** — bắt buộc, form dài mà nút Lưu trôi khỏi màn thì
  người dùng phải cuộn xuống đáy mới lưu được. Khung chuẩn:

  ```html
  <div class="offcanvas-body tk-oc">
    <form id="frm" …>
      <div class="tk-oc-fields"> …các trường… </div>
      <div class="tk-oc-actions">  <!-- ghim đáy -->
        <button type="submit" class="btn btn-primary me-2">Lưu</button>
        <button type="reset" class="btn btn-label-secondary" data-bs-dismiss="offcanvas">Huỷ</button>
      </div>
    </form>
  </div>
  ```

  Panel không có `<form>` (xem nhanh, tạo phiếu) thì đặt thẳng `.tk-oc-fields` + `.tk-oc-actions`
  làm con của `.offcanvas-body.tk-oc`. CSS nằm ở `wwwroot/css/tourkit.css`.
- **Trường lưu HTML** (nội dung bài viết, thân email) đặt class `tk-rte-field` lên `<textarea>`;
  `tk.form` tự dựng ô soạn thảo (`tk.rte`, chạy trên **Quill** — bộ soạn thảo đi kèm theme;
  dự án KHÔNG có TinyMCE). Textarea gốc vẫn giữ giá trị nên FormData/validate không đổi.
  Trường phụ thuộc kênh (Email = HTML, SMS/Zalo = văn bản thuần) thì gọi `tk.rte.enable(el, on)`
  trong `afterOpen` và ở sự kiện `change` của ô chọn kênh.

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
