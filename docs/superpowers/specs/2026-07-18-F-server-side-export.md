# Item F — Excel/CSV export server-side (all-pages)

**Ngày:** 2026-07-18 · **Effort:** S-M · **Trạng thái:** ✅ (Customers, mẫu tái dùng)

## Vấn đề
Export client hiện có (`exportRowsToCsv`) chỉ xuất **trang đang tải** trên bảng phân trang → thiếu dữ liệu khi nhiều trang. Cần export **toàn bộ** khớp bộ lọc.

## Thiết kế
- **Helper server** `CsvBuilder` (Application/Common): dựng CSV UTF-8+BOM, escape RFC4180 — tái dùng cho mọi endpoint export. Test 5 ca (BOM/header/dòng + escape).
- **Endpoint** `GET /customers/export?<filter>` [CustomerView]: gọi `ListAsync(1, int.MaxValue, filter)` (mọi trang, cùng bộ lọc list) → `CsvBuilder` → `File(bytes, "text/csv", "khach-hang.csv")`.
- **Helper client** `downloadBlob` (shared/exportCsv): tải Blob từ endpoint qua `<a download>`.
- **FE** CustomersPage: nút Xuất gọi endpoint server (responseType blob) → tải toàn bộ, thay export client trang-hiện-tại.

## Mẫu tái dùng (mở rộng sau)
Áp cùng pattern (CsvBuilder + endpoint `/export` + downloadBlob) cho Orders/Leads/Providers… khi cần. Customers là bản mẫu đã kiểm chứng. *(Excel .xlsx thật (EPPlus như legacy) = nâng cấp sau nếu cần format/nhiều sheet.)*
