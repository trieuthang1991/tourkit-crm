# TourKit — Rà soát nghiệp vụ & Phương án xử lý (PM)

**Bối cảnh.** Nghiệp vụ GỐC = legacy `tourkit` (.NET WebForms, 52 module business, đa tenant) tại `D:\MiGroup\tourkitapp\tourkit` — đây là **chuẩn nghiệp vụ**. Bản `tourkit-crm` (mới) đã dựng lại **UI Vuexy đồng nhất** + phần lớn CRUD, nhưng **nhiều logic nghiệp vụ sâu bị lược bỏ**, và vài module tích hợp **chưa có**.

**Kết luận PM.** Bản mới **hơn** ở nền tảng (multi-tenant SaaS, JWT refresh, RBAC catalog, auto-audit, template đa kênh, quote→order convert, model Template→Departure→Order→Seat, tier giá theo SL). Nhưng **thiếu** ở: (a) vài **lỗi đúng-sai** (giữ chỗ không tự nhả, trùng lịch HDV/xe, VAT báo giá), (b) **lõi thương mại** (payment-term, chốt đơn, công nợ NCC, hoa hồng bậc thang), (c) **quản trị** (tạo/sửa user, reset mật khẩu, roles UI, phòng ban cây, phân quyền theo phạm vi), (d) **tích hợp** (Zalo ZNS/SMS thật, BankHub đối soát, HĐĐT MiFi, CallCenter, Pancake).

---

## 1) Bản đồ phủ (52 module → feature). ✅ đủ · ⚠️ có UI thiếu logic · ❌ chưa có
CRM: CustomerData⚠️ · CustomerCare⚠️ · BaoGia⚠️(VAT!) · SampleTours⚠️ · GroupTour/SingleTour⚠️ · FeedBackTour⚠️ · CallCenter❌ · Pancake❌ · DuTruTours⚠️
Vận hành: BookingTour✅ · BookingHotel⚠️ · Hotel✅ · AirPlaneTicket⚠️ · BookingTicket⚠️(board) · CarManagement⚠️ · TourGuideManagement⚠️ · Calendar⚠️(stub) · LichTrinhTours⚠️ · Visa⚠️ · ServiceManager⚠️ · Provider⚠️(công nợ!)
Tài chính: Phiếu Thu/Chi⚠️(duyệt/thông báo/export) · MoneyReport⚠️ · InvoiceBranch⚠️ · BillMiFi❌ · ConfigCommission⚠️(bậc thang) · ReportCommission⚠️ · KPI⚠️(khác concept) · SystemReport⚠️ · BankHub❌
Nền tảng: Workflow⚠️(chưa nối) · Tasking⚠️ · Notification⚠️ · Email/EmailManager⚠️ · SMS⚠️(stub) · ZaloZns❌ · ZaloUID❌ · User⚠️(thiếu tạo/sửa) · Authen⚠️(roles/reset) · LogSystem✅ · WorkSpaceView⚠️ · HomePage⚠️

---

## 2) Gap nghiệp vụ trọng yếu (từ 4 nhóm phân tích)

**Lỗi đúng-sai (correctness):**
- **Giữ chỗ không tự nhả**: `HoldExpiresAt` được set nhưng KHÔNG có job reclaim → chỗ giữ "kẹt" mãi.
- **Trùng lịch HDV & xe**: assign guide/vehicle chỉ check tồn tại, KHÔNG chặn double-booking (legacy `HandleGuideInTour` chặn).
- **VAT báo giá**: `QuoteMath` không có VAT / tỉ giá / phụ thu → KHÔNG tái tạo được tổng có VAT cho hoá đơn VN.
- **Approval Method.All**: reject ngay ở phiếu đầu (legacy = bỏ phiếu tới khi hết Pending) — sai luật duyệt.
- **Approval config mồ côi**: màn `approvalProcesses` lưu template nhưng runtime lấy steps từ client DTO, KHÔNG snapshot template đã lưu.

**Lõi thương mại thiếu:**
- **ServicePaymentTerm** (lịch thanh toán NCC theo từng dịch vụ + đến hạn/quá hạn chi) — thiếu hẳn.
- **Chuỗi duyệt cấp đơn + chốt đơn** (duyệt booking→xuất HĐ→hoa hồng→chốt, status 108/109) — chỉ có duyệt phiếu chung.
- **Công nợ NCC + đối trừ + quyết toán HDV** (tra/aging/đối trừ, guide return thu-chi hộ) — thiếu.
- **Hoa hồng campaign + bậc thang** (chính sách theo mốc lợi nhuận, phạm vi user, chống chồng ngày) — chỉ có % phẳng.
- **Chia dòng Sale vs Điều hành** (1 dịch vụ → dòng bán + dòng chi NCC) — OrderCost hiện phẳng.
- **Thông báo người duyệt** (email/in-app tới bước kế/trước) — thiếu → luồng duyệt khó dùng thực tế.

**Quản trị thiếu:**
- **User**: không tạo/sửa/xoá user, không reset mật khẩu, không đổi trạng thái trong app.
- **Roles/Quyền**: không có màn Roles + gán quyền; mất mô hình quyền **theo phạm vi** (own/branch/group/all).
- **Phòng ban phẳng** (không cây ParentId), **quên/reset mật khẩu**, **logout server-side**, **lịch sử đăng nhập**, **IP whitelist** — thiếu.

**Tích hợp thiếu:** Zalo ZNS (OAuth/template/lịch/gửi batch) ❌ · Zalo UID ❌ · SMS gateway thật (đang stub) · BankHub (QR + webhook → tự tạo phiếu thu) ❌ · HĐĐT MiFi ❌ · CallCenter (OmiCall/CCall) ❌ · Pancake (inbox FB/Zalo) ❌.

**Báo cáo/Export:** **KHÔNG có Excel export** ở toàn bộ báo cáo/phiếu (legacy có EPPlus khắp nơi) · thiếu phân quyền phạm vi trên report · thiếu báo cáo đại lý / doanh thu dự kiến / theo thời gian duyệt / KPI target-vs-actual · MoneyReport thiếu nhánh FIT + hoàn huỷ chỗ.

**Cộng tác/Notification:** Notification 1 feed phẳng, 1 trigger (giao việc), không typed/actionable · Tasking mất multi-assignee/checklist/cây/@mention/My-Tasking · Workspace social feed rớt còn blog (không like/mention/reply lồng/upload).

---

## 3) PHƯƠNG ÁN XỬ LÝ (ưu tiên)

### P0 — Sửa NGAY (lỗi đúng-sai + chặn dùng thực tế). Nhỏ–vừa, làm được ngay:
| # | Việc | Vì sao | Effort |
|---|------|--------|--------|
| P0-1 | **Job tự nhả giữ chỗ hết hạn** (Hangfire quét `HoldExpiresAt < now` → trả chỗ) | Data sai, chỗ kẹt | S |
| P0-2 | **Chặn trùng lịch HDV & xe** khi assign (overlap ngày/giờ) | Điều động sai | S-M |
| P0-3 | **VAT + phụ thu + tỉ giá vào Báo giá** (QuoteMath + field + UI) | Không xuất HĐ VN đúng | M |
| P0-4 | **Sửa luật Approval Method.All** + **nối template→runtime snapshot** | Duyệt sai/mồ côi | M |
| P0-5 | **Thông báo bước duyệt** (in-app + email tới approver kế/trước) | Luồng duyệt dùng được | M |
| P0-6 | **Quản trị User**: tạo/sửa/khoá + reset mật khẩu + **màn Roles gán quyền** | Admin không quản được | L |

### P1 — Lõi thương mại & báo cáo (đợt kế):
ServicePaymentTerm + trạng thái theo phiếu thật · Chuỗi duyệt cấp đơn + chốt đơn · Công nợ NCC + đối trừ + quyết toán HDV · Hoa hồng campaign/bậc thang + báo cáo mốc · Chia Sale/Điều hành · Excel export + phân quyền phạm vi trên report · MoneyReport nhánh FIT/hoàn huỷ · Customer dedup + auto-chia lead + import + customer-360 · Notification typed/actionable + wiring trigger · BankHub đối soát (QR+webhook).

### P2 — Tích hợp & tính năng nâng cao:
Zalo ZNS/UID + SMS thật · HĐĐT MiFi · CallCenter · Pancake · FeedBackTour survey · KPI target-vs-actual · Tasking cộng tác (multi-assignee/checklist/@mention/My-Tasking) · Workplace social feed · Booking kanban + auto-assign + thống kê lý do huỷ · Combo pricing + allotment consumption · Phòng ban cây + phân quyền phạm vi · Widget dashboard điều hành còn thiếu.

**Khuyến nghị PM:** làm **P0 trước** (bounded, sửa lỗi + mở khoá quản trị), rồi P1 theo giá trị tài chính (payment-term, công nợ, hoa hồng, export), P2 là các epic tích hợp lớn (ước tính 3-4 tuần/tích hợp) — nên xếp lịch riêng.

---

## 4) Giao dev — ĐỢT P0: HOÀN TẤT ✅ (build sạch, test pass, migration áp, API live)
- **P0-1** Job tự nhả giữ chỗ hết hạn (`HoldReleaseJob` Hangfire 10'). ✅
- **P0-2** Chặn trùng lịch HDV & xe (overlap check trong Guide/VehicleAssignmentService). ✅
- **P0-3** VAT + phụ thu + tỉ giá vào báo giá (`QuoteMath`+`QuoteLine`+migration; tương thích ngược). ✅
- **P0-4** Sửa luật duyệt Method.All (vote tập thể, luôn kết thúc) + nối template→runtime snapshot (`approvalProcessId`+migration). ✅ (54/54 test)
- **P0-5** Thông báo người duyệt (in-app + email, best-effort). ✅ *(chờ: notify creator — cần field CreatedByUserId trên voucher)*
- **P0-6** Quản trị User (tạo/sửa/khoá/reset-MK) + Roles/gán quyền (API `RolesController`/`PermissionsController` + màn Users viết lại + `RolesPage` mới + route `/roles` + menu "Vai trò & quyền"). ✅
  - Lưu ý: gate quyền dùng `user.manage`/`user.view` (catalog chưa có `user.create/update/delete`). Nếu muốn tách quyền mịn hơn → thêm mã vào PermissionSeeder + backfill cho role Admin.

## 5) ĐỢT P1 — ĐANG TRIỂN KHAI (chủ động)
Đã xong (build sạch, test pass, migration áp, API live):
- ✅ **Xuất Excel/CSV** dùng chung (`shared/exportCsv` + `ExportButton`) wire 12 trang danh sách + báo cáo.
- ✅ **Payment-term NCC** (lịch thanh toán NCC): entity+migration+CRUD+due-alerts endpoint; trạng thái điều hành DV tính từ phiếu chi thật; UI drawer lịch thanh toán + widget đến hạn/quá hạn.
- ✅ **Công nợ NCC**: aging FIFO 0-30/30/60/90+ + endpoint lịch sử giao dịch/NCC; UI cột aging + modal drill-down. *(đối trừ/đối soát công nợ = hoãn, cần schema riêng.)*
- ✅ **Hoa hồng bậc thang**: CommissionCampaign+Tier+CampaignUser (migration), overlap validation, `/resolve` rate; UI CRUD đầy đủ. **Report "Hoa hồng theo mốc"** (`GET /reports/commission-by-milestone`) bám legacy `ReportCommissionByMilestone`: % từ bậc lợi nhuận, xuất HH theo lợi nhuận LẪN doanh thu, lọc khoảng ngày + Excel/CSV + route/menu. ✅ (Item A) *(chờ: wire tier vào order-commission lúc chốt đơn — gộp Item B.)*

- ✅ **Cổng chốt/tất toán đơn** (Item B): `OrderStatus.Closed` + `POST /orders/{id}/close|reopen`, gate tuần tự (Confirmed + IsPaymentRecognized + IsCommissionSettled), audit ClosedAt/ClosedByUserId, khoá AssignSales khi Closed; UI nút Tất toán/Mở lại ở OrderDetail + nhãn/màu. *(workflow duyệt đơn đa cấp = hoãn theo quyết định PM; khoá mutation cấp chỗ = follow-up.)*

- ✅ **Notification typed/actionable** (Item C1): `Notification.Type` + migration, `PushAsync(type)`, gán type tại emit (task/approval), FE icon+nhãn theo loại. *(C2 notify-creator = hoãn có kiểm soát: cần StartedByUserId trên approval + đụng state machine duyệt.)*

- ✅ **Báo cáo thu chi theo loại tour** (Item D): `GET /reports/money-by-tour-type` gom theo BookingType (FIT/GIT…), trừ TotalRefund ra doanh thu ròng + UI/Excel/menu. *(không tái tạo FIT-sub-booking legacy — model khác.)*

- ✅ **Rà khách trùng** (Item E dedup): `GET /customers/duplicates` gom theo SĐT/email chuẩn hoá + UI/menu. *(auto-chia lead/import/360/merge = tách slice riêng.)*

- ✅ **Export server-side all-pages** (Item F): `CsvBuilder` (Application/Common) + `GET /customers/export` (mọi trang, cùng bộ lọc) + `downloadBlob` client; CustomersPage xuất toàn bộ. Mẫu tái dùng cho Orders/Leads…

Còn lại **P1** (slice tách, PM quyết): [E: auto-chia lead + import + 360 + merge] · [C2 notify-creator] · [mở rộng export F sang Orders/Leads] · [wire tier hoa hồng vào order-commission lúc chốt] · [đối trừ/đối soát công nợ NCC — cần schema].

## 6.5) P2 (epic tích hợp ~3-4 tuần/cái): Zalo ZNS/UID · BankHub đối soát · HĐĐT MiFi · CallCenter · Pancake · FeedBackTour survey · KPI target-vs-actual · Tasking cộng tác · Workplace social feed.

## 6) Dữ liệu test performance
Đã seed ~3000/bảng (KH/Đơn/Lead/Báo giá/Phiếu thu) + ~79k activity log qua `SEED_PERF_COUNT=3000`. (Còn dở: sweep tìm page LỖI thật — cần dọn khoá profile chrome-devtools.)
