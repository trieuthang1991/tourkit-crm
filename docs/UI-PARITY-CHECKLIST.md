# Quy trình đối chiếu giao diện cũ (parity) — TRÁNH MISS THÔNG TIN

> Lý do: các màn Razor sinh nhanh đợt đầu chỉ có 4–6 cột "đủ chạy", trong khi bản cũ
> (`web/src/features/**/*Page.tsx`) có KPI, thanh lọc nhiều tiêu chí, cột kép, dòng tổng
> cộng, export… Khách dùng bản mới sẽ thấy **mất dữ liệu**. Mọi màn PHẢI đối chiếu trước khi coi là xong.

## Nguồn chuẩn

| Nguồn | Dùng để |
|---|---|
| `web/src/features/<module>/<X>Page.tsx` | **Chuẩn thông tin**: KPI, bộ lọc, cột, hành động, tổng cộng, export |
| `docs/vuexy-reference/**` | **Chuẩn giao diện**: markup/khuôn card, table, offcanvas, modal |
| Service trong `src/TourKit.Application/**` | **Chuẩn dữ liệu**: field DTO, tham số filter có thật |

## 6 bước bắt buộc cho mỗi màn

1. **Đọc trang cũ** `web/src/features/.../XPage.tsx`. Ghi ra 5 nhóm:
   - `kpiItems` / StatGrid → **thẻ thống kê** (đủ số thẻ, đúng nhãn)
   - thanh lọc (`SearchInput`, `Select`, `DateRangeInput`, panel "Lọc nâng cao") → **đủ tiêu chí**
   - `columns: Column<T>[]` → **đủ cột**, kể cả cột ghép (2 dòng) và cột STT
   - `summary` / tfoot → **dòng tổng cộng**
   - nút: export, thêm, sửa, xoá, hành động nghiệp vụ riêng
2. **Đối chiếu filter DTO** trong Application. Tiêu chí nào service có → đẩy xuống SQL.
   Tiêu chí nào service KHÔNG có → **bỏ, ghi rõ trong báo cáo** (không lọc client giả vờ).
3. **Server-side paging bắt buộc** với bảng nghiệp vụ (xem `no-get-all-server-paging`):
   `TkListPageModel` + `OnGetDataAsync` + `tk.table`. Danh mục nhỏ mới được `tk.tableClient`.
4. **Giữ nguyên** perm `[Authorize]`, offcanvas thêm/sửa, convention form (placeholder, dấu *,
   jQuery Validate, flatpickr, Select2, SweetAlert2, Result envelope).
5. **Build + verify**: `dotnet build`, chạy `:5075`, gọi `?handler=Data` kiểm `recordsTotal`/số dòng,
   mở trình duyệt kiểm 0 lỗi console + offcanvas prefill.
6. **Báo cáo**: cột/bộ lọc nào đã bù, cái nào bỏ và VÌ SAO.

## Mẫu chuẩn đã làm (copy y hệt)

| Kiểu màn | File mẫu |
|---|---|
| List server-side đầy đủ (KPI + lọc nâng cao + cột kép + tổng cộng + export) | `src/TourKit.Api/Pages/Orders/Index.cshtml(.cs)` |
| List server-side + offcanvas CRUD | `src/TourKit.Api/Pages/WorkTasks/Index.cshtml(.cs)` |
| Dashboard/widget theo khuôn Vuexy | `src/TourKit.Api/Pages/Dashboard/Index.cshtml(.cs)` |

## Bẫy đã gặp

- `tk.table` đặt `ordering:false` → sắp xếp do server quyết; đừng dựa `order:[[...]]`.
- Bỏ `data-json` trên `<tr>`: server-side lấy dữ liệu dòng bằng `dt.row($(this).closest('tr')).data()`.
- Trả thêm khối tổng ngoài contract DataTables thì đọc qua sự kiện `xhr.dt` (`json.pageSum`).
- Npgsql chỉ nhận `DateTimeOffset` offset 0 → luôn `.ToUniversalTime()` khi parse ngày lọc.
- Analyzer bật `TreatWarningsAsErrors`: không using thừa, braces mọi `if`, `CultureInfo.InvariantCulture`.
