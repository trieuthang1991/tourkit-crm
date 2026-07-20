# Wave 1 — Xương sống bán hàng → dòng tiền (plan chi tiết theo màn)

> Điều kiện: Đợt 1 + Đợt 2 (Wave 0) xong. Mỗi màn = 1 task độc lập, làm TUẦN TỰ theo thứ tự dưới (màn sau dùng dữ liệu màn trước). Mỗi màn TRƯỚC KHI CODE phải soi màn tương ứng hệ cũ (`D:/MiGroup/tourkitapp/tourkit` + staging) như đã làm với form Khách hàng — form nào input/select/multi, trường động gì, cột list nào.

**Chuẩn chung mọi màn (không nhắc lại từng task):** dùng `_TkTable`/`_TkOffcanvas`/`tk.js` + `ITkLookupService`; theo `docs/UI-CONVENTIONS.md` (placeholder, dấu *, jQuery Validate, flatpickr, Select2, Swal, Result envelope, fix width cột, search dùng SearchName/PhoneNormalized pattern); `[Authorize(Policy=...)]` đúng perm; nối leaf menu `_MenuData.cs` từ `#` → route thật; verify Chrome + test không hồi quy + commit riêng từng màn.

---

## 1.1 CRM — Cơ hội bán hàng (Leads) `/Leads`
- **API/Service:** `LeadsController` → `ILeadService` (đã có; sửa search theo pattern SearchName khi chạm).
- **Màn:** list DataTables (cột: tên/SĐT/nguồn/chiến dịch/NV được chia/trạng thái/ngày) + offcanvas thêm-sửa + hành động **Convert → Customer** (nút trên dòng, Swal confirm, gọi endpoint convert có sẵn).
- **Lookup:** Users (NV), LeadCampaigns, CustomerSources.
- **Soi hệ cũ:** `CMS/KojiCRM/Modules/CustomerData/Templates/LeadCustomer/*` — đối chiếu cột list + trạng thái lead + luồng chia số.
- **Nghiệm thu:** tạo lead → chia NV → convert thành khách (khách xuất hiện ở /Customers, lead đổi trạng thái); search không dấu.

## 1.2 CRM — Chia số Sale (LeadCampaigns) `/LeadCampaigns`
- CRUD chuẩn S (khung Wave 0) + panel gán NV + tỉ lệ chia (bám `ListSettingScale.aspx` legacy).
- **Nghiệm thu:** tạo campaign, gán 2 NV tỉ lệ, lead mới vào campaign được chia đúng.

## 1.3 CRM — Quản lý lịch hẹn / CSKH (CustomerCares) `/CustomerCares`
- **Màn:** list theo khoảng ngày + trạng thái + NV; offcanvas tạo lịch hẹn (chọn khách qua **suggestion** Wave 0 Task 4 — autocomplete SĐT/tên như legacy `customer-suggestion`); nút hoàn thành/ghi kết quả.
- **Soi hệ cũ:** `Modules/CustomerCare/*` (CustomerCareVM, TypeSchedule, nhắc hẹn).
- **Nghiệm thu:** tạo hẹn cho khách → hiện ở list + timeline trang chi tiết khách; job nhắc hẹn (CareReminderJob có sẵn) không vỡ.

## 1.4 Rà khách trùng `/Customers/Duplicates`
- List nhóm trùng (phone/email) từ `FindDuplicatesAsync` (Đợt 1 đã tối ưu PhoneNormalized). Bảng nhóm mở rộng được, link sang chi tiết từng khách.
- **Nghiệm thu:** 2 khách cùng SĐT (0901 vs +84901) hiện chung 1 nhóm.

## 1.5 Báo giá — calculator (Quotes) `/Quotes` (+7 biến thể) — **L, màn nặng nhất wave**
- **Bắt buộc soi kỹ hệ cũ trước** (memory `legacy-nghiepvu-4-module`): calculator 3 nhóm chi phí + công thức giá; template Excel `chiet_tinh_tour_v*.xlsx`; React `quotes/QuotesPage.tsx` + `QuoteLinesField`.
- **Chia nhỏ:** (a) list báo giá + filter loại; (b) form tạo/sửa = **trang riêng** (không offcanvas — form quá lớn): thông tin chung + bảng dòng chi phí (thêm/xoá dòng, jQuery tính tổng realtime: giá vốn/giá bán/lãi theo nhóm khách người lớn-trẻ em); (c) 7 biến thể (`quoteType`) = 1 khung + cấu hình cột/nhóm chi phí; (d) bản in `_PrintLayout` (Wave 0 Task 5 hoàn tất tại đây); (e) chuyển báo giá → Đơn hàng (nút, sang 1.6).
- **Nghiệm thu:** nhập báo giá tour 2 nhóm chi phí, tổng khớp tay tính; in ra A4 sạch; convert ra order giữ số liệu.

## 1.6 Đơn hàng — list (Orders) `/Orders` — **L**
- **API:** `BookingController` (+filter Branches/Users/Departments/MarketTypes/TourGroups + bookingType). React `booking/OrdersPage.tsx` (22K) là spec cột/filter — liệt kê đủ trước khi code.
- **Màn:** DataTables + thanh filter mở rộng (loại tour qua query `?bookingType=` cho 6 leaf menu) + thẻ thống kê + hành động dòng (sửa trạng thái nhanh, vào detail).
- **Nghiệm thu:** 6 leaf menu (FIT/GIT/LandTour/Visa/DV lẻ/Tất cả) lọc đúng; search mã đơn/tên khách không dấu; phân trang SQL với 3000 đơn seed.

## 1.7 Đơn hàng — chi tiết (OrderDetail) `/Orders/Details/{id}` — **L, nhiều panel nhất**
- Khung `_TkDetailShell` + pills: **Tổng quan** (header đơn + khách + chuyến) · **Chi phí** (OrderCosts) · **Phụ thu** (OrderSurcharges) · **Thu** (ReceiptsPanel) · **Chi** (PaymentsPanel) · **Hoa hồng** (CommissionPanel) · **Chuyển khách** (TourTransfers). Mỗi panel = partial + handler AJAX riêng — làm 7 panel thành 7 bước commit riêng.
- **Hợp đồng in:** `/Orders/{id}/Contract` qua `_PrintLayout` (OrderContractsController).
- **Nghiệm thu:** đơn seed mở đủ 7 panel đúng số liệu; thêm 1 chi phí → tổng cập nhật; in hợp đồng.

## 1.8 Chuyến đi (Departures) `/Departures` + Detail — **L**
- List chuyến + trạng thái chỗ (giữ/đặt/còn) + **Calendar view** (fullcalendar Vuexy — viết `tk.calendar` wrapper tại đây) + tạo loạt chuyến (BatchDeparture — offcanvas ngày lặp). Detail: danh sách khách/chỗ của chuyến (TourCustomer), thao tác giữ chỗ/nhả chỗ (P0 hold đã có backend + job).
- **Nghiệm thu:** tạo loạt 4 chuyến tháng sau; calendar hiện đúng; giữ chỗ rồi HoldReleaseJob nhả đúng hạn (kiểm bằng log).

## 1.9 Điều hành calendar `/OperationsCalendar`
- Tái dùng `tk.calendar` (1.8) đổi nguồn dữ liệu — màn mỏng.

## 1.10 Tài chính — Phiếu thu (Receipts) `/Receipts` + Phiếu chi (Payments) `/Payments`
- 2 màn cùng khung: list + filter (khoảng ngày/trạng thái/chi nhánh/NV) + thẻ tổng · offcanvas tạo phiếu (chọn đơn qua suggestion, số tiền, tài khoản PaymentAccounts, flatpickr ngày) · **panel duyệt** (ApprovalPanel — trạng thái bước duyệt, nút duyệt/từ chối ghi lý do) · view "Phiếu thu chờ" = filter trạng thái chờ (leaf fi-waiting).
- **Soi hệ cũ:** luồng duyệt (ReceiptApprovals/PaymentApprovals + ApprovalProcesses).
- **Nghiệm thu:** lập phiếu thu cho đơn → công nợ đơn giảm tương ứng (panel Thu ở OrderDetail khớp); duyệt 2 bước chạy đúng.

## 1.11 Hoá đơn VAT (Invoices) `/Invoices`
- List + form hoá đơn nhiều dòng (InvoiceLines — bảng dòng thêm/xoá như QuoteLines, tái dùng JS) + trạng thái phát hành. (P0 VAT đã fix backend.)
- **Nghiệm thu:** xuất hoá đơn từ đơn hàng, tiền thuế khớp.

## Chốt Wave 1
- Toàn luồng demo: Lead → convert Khách → Báo giá → Đơn → Chuyến → Phiếu thu → duyệt → Hoá đơn — chạy trơn end-to-end trên UI mới, không cần mở React.
- **S4 Global search navbar** làm CUỐI wave (đủ 4 loại thực thể có màn): endpoint gộp Customer/Order/Quote/Departure (Take 5, theo quyền) + dropdown debounce 300ms → click nhảy trang chi tiết tương ứng.
- Test suite xanh; cập nhật memory tiến độ.
