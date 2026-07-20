# TourKit — Master Plan: Database · UI Migration · Search

> Ngày lập: 2026-07-20 · Nhánh: `feat/vuexy-theme` · Trạng thái nền: đã xong vertical slice Khách hàng (Razor Pages Vuexy gộp trong TourKit.Api, auth cookie, DataTables server-side, offcanvas, trang chi tiết) + bộ quy ước `docs/UI-CONVENTIONS.md`.

---

# PHẦN A — ĐÁNH GIÁ THIẾT KẾ DATABASE

## Kết luận nhanh

**Nền móng ĐẠT — kiến trúc đúng hướng, nhưng CHƯA sẵn sàng scale**: có 2 vấn đề Critical về hiệu năng (tính toán in-memory) và một loạt lỗ hổng index/unique/toàn vẹn tham chiếu cần vá **trước khi** nhân bản UI hàng loạt (vì mọi màn list mới sẽ copy pattern truy vấn hiện tại).

## Những gì đã tốt (giữ nguyên)

- ~91 entity phủ đủ domain (CRM, Tour/Ops, Order, Finance, Commission, Flight/B2B, Workflow, Billing, CMS).
- Multi-tenancy chặt: global query filter `TenantId + !IsDeleted`, chặn ghi chéo tenant, soft-delete nhất quán.
- Audit tự động (ActivityLog + diff JSON qua interceptor).
- Index prefix `TenantId` đúng convention; decimal (18,2)/(18,4) khai báo rõ; `DateTimeOffset` nhất quán; `jsonb` thật cho CrmProfile; unique theo tenant phủ tốt catalog.

## Vấn đề cần sửa (xếp ưu tiên)

| # | Mức | Vấn đề | Vị trí | Cách sửa |
|---|---|---|---|---|
| C1 | **Critical** | `GetStatsAsync`/`GetFunnelAsync` load TOÀN BỘ bảng Customer + Order vào RAM để đếm | `CustomerService.cs:118-179` | Đẩy `COUNT/GROUP BY` xuống SQL (thêm `CountAsync`/aggregate vào `IRepository` hoặc query `IQueryable` trực tiếp) |
| C2 | **Critical** | `ListAsync` phân trang in-memory (kéo hết candidates rồi mới Skip/Take); kèm load hết Users mỗi request | `CustomerService.cs:26-87` | `Skip/Take` ở SQL khi không có filter jsonb; đẩy filter jsonb xuống Postgres (`->>`); dict Users cache |
| H2 | High | Customer thiếu index Phone/Email/Code/Source (search + dò trùng seq-scan) | `CustomerConfiguration.cs:26` | Thêm `HasIndex(TenantId, Phone/Email/Code)`; kết hợp Phần C (trigram) |
| H3 | High | KHÔNG unique: `Customer.Code`, `Order.Code`, `Agent.Code`, `FlightTicket.Pnr` | các Configuration | `HasIndex(TenantId, Code).IsUnique()` |
| H4 | High | Quan hệ trong JSON bằng string (CreatedBy/AssignedTo user-id string, Branch/Department bằng TÊN) — mất toàn vẹn, join tay in-memory | `Customer.CrmProfileJson` | Tách `CreatedByUserId` FK + bảng nối `CustomerAssignee`; field mềm còn lại giữ JSON |
| H5 | High | `Customer.TempBalance` decimal KHÔNG precision (nguy cơ mất phần thập phân) | `CustomerConfiguration.cs` | `HasPrecision(18,2)` |
| M1 | Medium | Catalog liên kết bằng TÊN (Source/Tag) nhưng CustomerType bằng Code — không nhất quán; rename catalog làm mồ côi data | `Customer.cs` | Chọn 1 cơ chế: FK Id (chuẩn) hoặc quy trình đồng bộ khi rename |
| M2 | Medium | `Tag` (cột đơn) vs `Tags` (list trong JSON) — 2 nguồn sự thật | `Customer.cs:12` + JSON | Hợp nhất về `Tags` (JSON), cột `Tag` chỉ là cache hiển thị hoặc bỏ |
| M3 | Medium | Dò trùng quét toàn bảng in-memory | `CustomerService.cs:354-377` | Cột `PhoneNormalized` persisted + index, group ở SQL |
| M4 | Medium | Index `TourDeparture.IsClosed` không prefix TenantId | `TourDepartureConfiguration.cs:17` | Sửa/bỏ |
| L1-L4 | Low | LIKE '%%' không sargable; FilterOptions distinct in-memory; jsonb chỉ Postgres; audit serialize mọi entity | — | Xử lý cùng Phần C + khi chạm tới |

**Hành động đề xuất: "Đợt DB Hardening"** (1 đợt gọn, làm TRƯỚC khi nhân bản UI):
1. Migration thêm index + unique (H2, H3, M4) + precision (H5) — rẻ, không đụng code.
2. Sửa C1/C2 trong CustomerService + bổ sung `CountAsync`/aggregate cho `IRepository` — làm chuẩn mẫu để mọi service sau copy đúng.
3. Cột `PhoneNormalized` + `SearchName` (xem Phần C — chung 1 migration).
4. H4/M1/M2 (chuẩn hoá quan hệ) để đợt riêng sau — cần migrate data, rủi ro cao hơn, không chặn UI.

---

# PHẦN B — KẾ HOẠCH CHUYỂN UI SANG RAZOR PAGES VUEXY

## Hiện trạng & khối lượng

- **Đã xong (4):** Login, Dashboard (placeholder), Customers Index + Details — là **pattern mẫu** đầy đủ (DataTables server-side, offcanvas Select2/flatpickr/jQuery-Validate, SweetAlert2, Result envelope).
- **Còn lại (~68 màn đích** từ ~85 route React): **S ≈ 27** (CRUD chuẩn — nhân bản pattern) · **M ≈ 28** (nhiều filter/tab/panel) · **L ≈ 13** (đặc thù: calculator báo giá, 2×Gantt, Kanban, Calendar, quỹ phòng allotment, Orders list+detail, Departures, Tour builder, Dashboard chart, Workspace, HH bậc thang).
- 75/75 controller đều có màn/panel dùng — không có API mồ côi.
- Gap chưa từng có ở React (quyết sau, không chặn): Mạng nội bộ, Zalo OA/ZNS/UID, Feedback ZNS riêng.

## Wave 0 — Bộ khung dùng lại (điều kiện tiên quyết)

Trích xuất từ Customers thành **partial/tag-helper/JS module dùng chung** — quyết định tốc độ toàn dự án:
- `_DataTablePage` (khung list: thẻ thống kê + filter bar + DataTables server-side + width cố định + ngôn ngữ VN)
- `_CrudOffcanvas` (khung offcanvas: Select2/flatpickr/jQuery-Validate/AJAX-Result/Swal)
- Lookup selects dùng chung (Users, Branches, Departments, MarketTypes, Providers, Customers-suggestion)
- Khung Detail (card hồ sơ + nav-pills + timeline), khung Print, Chart wrapper (ApexCharts có sẵn Vuexy)
- Các khung L làm khi chạm wave tương ứng: Gantt/ScheduleBoard, Calendar (fullcalendar có sẵn Vuexy), Kanban (sortablejs có sẵn), Allotment grid

## Wave 1 — Xương sống bán hàng → dòng tiền (ưu tiên cao nhất)

Khách → Báo giá → Đơn → Chuyến → Thu/Chi:
1. CRM: Leads + Lead campaigns, Customer cares, Rà khách trùng
2. **Quotes calculator (L)** — 1 khung 7 biến thể (bám công thức hệ cũ, memory `legacy-nghiepvu-4-module`)
3. **Orders list (L) + OrderDetail (L)** (đa panel: costs/surcharges/transfers/commission/receipts/payments) + hợp đồng in
4. **Departures (L)** + DepartureDetail + Operations calendar
5. Tài chính: Receipts + Payments (kèm duyệt), Invoices VAT

## Wave 2 — Cung ứng & điều hành

Providers, ServiceItems/ProviderServices, ServiceBookings + lịch thanh toán, ServiceOperations, **RoomFund allotment (L)**, FlightTickets (đoàn + lẻ), Guides (bảng + **Gantt L**), Vehicles (bảng + **Gantt L**).

## Wave 3 — Danh mục & cấu hình (~27 màn S, throughput cao)

ConfigHub + toàn bộ catalog CRUD — nhân bản pattern Customers hàng loạt (có thể dùng subagent song song). Nhanh, rẻ, chèn song song wave khác khi rảnh.

## Wave 4 — Quản trị & báo cáo nâng cao

Users, Roles/ma trận quyền, Billing, ApprovalProcesses, Commission (rules + **campaigns bậc thang L**), toàn bộ Reports (KPI/Turnover/HH), **Dashboard CeoAnalytics (L)**, **Workspace (L)**.

## Wave 5 — Phụ trợ & mở rộng

Marketing campaigns, Posts/Categories/Comments, B2B (Agents/AgentBookings/AgentQuotes), **TourTemplate builder (L)**, Landing public, Registration. Cuối cùng: **gỡ hẳn `web/` React + CORS/SPA config**.

**Nguyên tắc xuyên suốt:** mỗi màn theo `docs/UI-CONVENTIONS.md`; verify chạy thật (Chrome) + test không hồi quy trước khi sang màn kế; menu `_MenuData.cs` nối leaf `#` → route thật dần theo wave.

---

# PHẦN C — THIẾT KẾ SEARCH (tiện nhất cho người dùng)

## Phát hiện chính

| Tiêu chí | Legacy (chuẩn nghiệp vụ) | Bản mới hiện tại |
|---|---|---|
| Bỏ dấu tiếng Việt | **CÓ** (`COLLATE Latin1_General_CI_AI`) — gõ "nguyen" ra "Nguyễn" | **KHÔNG** — đây là tụt lùi so bản cũ |
| Hoa/thường | CI | Case-sensitive (LIKE Postgres) ở Customers |
| Trường search KH | tên/email/SĐT/mã/địa chỉ/dịch vụ | tên/mã/SĐT/email |
| SĐT +84 vs 0 | không chuẩn hoá | có hàm nhưng chỉ dùng dò trùng |
| Suggestion | CÓ (jQuery UI, minLength 3, top 10, theo form) | không |
| Global search navbar | không có | input chết chưa wire |
| Pattern code | — | **2 kiểu lẫn lộn**: SQL-side (case-sensitive) vs in-memory (tải cả bảng) ở 11+ service |

## Thiết kế đề xuất (theo thứ tự triển khai)

**S1 — Hạ tầng search không dấu (Postgres):** bật `unaccent` + `pg_trgm`; thêm cột generated `SearchName = lower(f_unaccent(FullName))` (bản Postgres của cột không-dấu legacy) + index `GIN gin_trgm_ops`; thêm cột `PhoneNormalized` (0-prefix, chỉ số) + index. Chọn **pg_trgm/ILIKE** thay tsvector vì nghiệp vụ là khớp substring (một phần tên/SĐT/mã), tsvector không hỗ trợ tốt.

**S2 — Search thông minh 1 ô (áp cho Customers trước, làm mẫu):** input toàn số → chuẩn hoá rồi khớp `PhoneNormalized` (gõ `0901` ra cả khách lưu `+84901…`); có chữ → `SearchName ILIKE '%unaccent(q)%'` + mã KH. Thêm địa chỉ vào phạm vi search (bám legacy).

**S3 — Chuẩn hoá pattern search toàn Application:** một helper/extension duy nhất; xoá dần kiểu "ListAsync() rồi lọc RAM" ở 11+ service — chuyển theo từng wave UI khi chạm tới service đó.

**S4 — Global quick-search trên navbar (nâng cấp so legacy):** wire ô "Tìm kiếm..." → endpoint `?handler=` hoặc `/api/search?q=` gộp đa thực thể (Khách/Đơn/Báo giá/Chuyến — mỗi loại Take 5, lọc theo quyền), debounce ~300ms, minLength 2-3 (khớp chuẩn legacy), dropdown nhóm theo loại → click nhảy thẳng trang chi tiết. Legacy chỉ có suggestion theo form; đây là điểm "tiện hơn bản cũ" rõ nhất.

**S5 — Suggestion theo ngữ cảnh form (bám legacy):** khi làm màn Booking/Order (Wave 1) — ô chọn khách autocomplete SĐT/tên, auto-fill thông tin, gate theo quyền (như `customer-suggestion` legacy).

---

# LỘ TRÌNH TỔNG HỢP (thứ tự thực thi đề xuất)

| Đợt | Nội dung | Vì sao trước |
|---|---|---|
| **1. DB Hardening + Search infra** | Vá C1/C2 + index/unique/precision + unaccent/pg_trgm + SearchName/PhoneNormalized (S1) + search thông minh Customers (S2) | Mọi màn UI sau copy pattern truy vấn — phải chuẩn từ gốc; migration gộp 1 lần |
| **2. Wave 0** | Trích components dùng chung từ Customers | Quyết định tốc độ mọi wave sau |
| **3. Wave 1 + S4/S5** | Xương sống bán hàng→dòng tiền + global search + suggestion form | Giá trị nghiệp vụ cao nhất |
| **4. Wave 2** | Cung ứng & điều hành (Gantt/Allotment) | Sau khi có đơn/chuyến |
| **5. Wave 3** | ~27 màn danh mục S (chèn song song được) | Rẻ, không chặn |
| **6. Wave 4** | Quản trị + báo cáo + dashboard | Cần data từ wave trước |
| **7. Wave 5 + dọn dẹp** | Phụ trợ/B2B + **gỡ web/ React** + H4/M1/M2 (chuẩn hoá FK) | Kết thúc migration |

**Rủi ro chính & đối sách:** (1) 13 màn L là 80% effort thật — mỗi màn L cần soi kỹ nghiệp vụ hệ cũ trước khi code (như đã làm với form Khách hàng); (2) sửa C2 đổi hành vi filter jsonb — cần giữ 592 test xanh + test thêm cho filter; (3) migration generated column cần `f_unaccent` IMMUTABLE wrapper — test kỹ trên Postgres local trước.
