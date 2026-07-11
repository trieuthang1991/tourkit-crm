# Roadmap bù khoảng trống so với hệ cũ (KojiCRM/TourKit legacy)

> Nguồn: đối chiếu 53 module hệ cũ (`CMS/KojiCRM/Modules/`) với hệ mới. Hệ mới hiện phủ **lõi đặt tour → thu tiền → duyệt** (~11/53 module). Roadmap này phân đợt theo **giá trị vận hành / mức chặn**, không nhất thiết làm hết 53 module.

Nguyên tắc: **usable-first** — vá thứ chặn vận hành trước, mở rộng phạm vi sau. Mỗi đợt ra được phần mềm chạy + test xanh, commit từng task.

---

## Đợt 0 — Vá lỗ hổng chặn vận hành (nhỏ, gấp) ⚡ ✅ ĐÃ XONG

Thứ đã có gần đủ nhưng thiếu mảnh khiến không dùng thật được. **Hoàn tất — 116 backend test + 46 web test xanh.**

| # | Việc | Trạng thái |
|---|---|---|
| 0.1 | **Chống overbooking** — kiểm tra tổng chỗ đã dùng (seat `Status==0`) + `BookingMath.SeatCount`, chặn vượt `TotalSlots` | ✅ |
| 0.2 | **Đóng chuyến** — `POST /tour-departures/{id}/close` (set `IsClosed/ClosedAt`) + chặn đặt chỗ/đóng lại | ✅ |
| 0.3 | **MarketType** full CRUD qua dispatcher (thêm Update/Delete) + UI sửa/xoá | ✅ |
| 0.4 | **Mở chuyến kế thừa** số chỗ/loại/lịch trình từ tour mẫu | ✅ |

> Ghi chú: overbooking hiện chặn ở tầng ứng dụng (đủ cho SQLite dev). Concurrency-proof tuyệt đối (RowVersion/counter) = backend-architecture.md bước 8, làm khi lên quy mô.

---

## Đợt 1 — Tài chính chi (Payment) 💸 ✅ ĐÃ XONG

Công ty tour không vận hành được nếu chỉ có thu mà không có chi. **Hoàn tất — 120 backend + 48 web test xanh; smoke test SQLite thật OK.**

- ✅ **Phiếu chi (`PaymentVoucher`)**: entity riêng bám `N_PaymentVoucher` (Code/Method/Amount/Receiver/ProviderId/OrderCostId/IsRecognized) + migration.
- ✅ **Duyệt chi 1 cấp**: tạo/duyệt/từ chối/liệt kê (CQRS mirror phiếu thu); chỉ ghi nhận dòng tiền khi duyệt.
- ✅ **Công nợ phải trả NCC**: `GET /reports/provider-debt` = Σ OrderCost.ActualAmount − Σ phiếu chi đã ghi nhận, theo provider.
- ✅ **Frontend**: panel phiếu chi trong chi tiết đơn + trang Công nợ NCC.
- ◻️ (Sau) Duyệt chi **nhiều cấp** (mirror ReceiptApproval) · **Dự trù chi phí tour (`DuTruTours`)**.

## Đợt 2 — Báo cáo & Dashboard 📊 ✅ ĐÃ XONG

Ưu tiên báo cáo quản trị hằng ngày. **Hoàn tất — 123 backend + 48 web test xanh; smoke SQLite thật đúng số.**

- ✅ **Dashboard tổng quan** (`/reports/dashboard`): số đơn, doanh thu, đã thu, còn phải thu, chi phí, đã chi, còn phải trả, lợi nhuận gộp — thành trang chủ.
- ✅ **Dòng tiền theo phương thức TT** (`/reports/cash-flow`): thu vào − chi ra − ròng, gom theo phương thức (chỉ tính phiếu đã ghi nhận).
- ✅ **Doanh thu–lợi nhuận theo đơn** (`/reports/turnover`): revenue − cost = profit mỗi đơn.
- ✅ Công nợ phải trả NCC (đã làm Đợt 1).
- ◻️ (Sau) Lợi nhuận/hoa hồng **theo nhân viên** → Đợt 3; báo cáo theo KH/đại lý, số dư tài khoản, KPI.

## Đợt 3 — Hoa hồng sales đúng nghĩa 🎯 ✅ ĐÃ XONG

Tách bạch với `ProfitShare` (chia lợi nhuận). **Hoàn tất — 131 backend + 51 web test xanh; smoke SQLite đúng số (hoa hồng 700k = LN 7tr × 10%).**

- ✅ **`Order.SalesUserId`**: gán sales phụ trách đơn (`PUT /orders/{id}/sales`).
- ✅ **`CommissionRule`** (=legacy `Comission`): CRUD quy tắc hoa hồng theo user (%).
- ✅ **Báo cáo hoa hồng/lợi nhuận theo NV** (`/reports/commission-by-user`): turnover/cost/profit + hoa hồng = profit × rate.
- ◻️ (Deferred) hoa hồng theo **loại khách** (`id_customer_type`) + `CommissionCampaign` — chờ `Customer.CustomerType`; **chốt sổ hoa hồng** (`StatusComission`/`date_closed`).

## Đợt 4 — CSKH sau tour & CRM sâu 🤝 ✅ ĐÃ XONG (phần dữ liệu)

**Hoàn tất — 140 backend + 55 web test xanh; smoke SQLite OK (care 201, rating chặn stars>5).**

- ✅ **CSKH (`CustomerCare`)**: CRUD lịch/nội dung chăm sóc + nhắc hẹn + phản hồi.
- ✅ **Đánh giá sau tour (`TourRating`, =legacy `Rate`)**: CRUD số sao (1-5) + nhận xét.
- ◻️ (Deferred) **Gửi thật** Email/SMS/Zalo (cần tích hợp provider ngoài) · `Tasking`/`Notification`/`CallCenter`/`Pancake` (tuỳ nhu cầu).

## Đợt 5 — Dịch vụ lẻ (không chỉ tour trọn gói) 🧳 🟡 LÕI ĐÃ XONG

Phần lõi groundable đã làm. **149 backend + 59 web test xanh; smoke SQLite OK.**

- ✅ **Catalog dịch vụ (`ServiceItem`, =legacy `services`)**: CRUD danh mục dịch vụ (phòng/xe/vé/visa...).
- ✅ **Bảng giá NCC (`ProviderService`, =`provider_services`+`provider_service_pricing`)**: CRUD giá hợp đồng/công bố theo NCC, lọc theo provider. Lấp gap `OrderCost.ServiceName` free-text.
- ✅ **Dự trù giá tour trong Báo giá** (lõi `BaoGia`/`DuTruTours` — spec `docs/superpowers/specs/2026-07-11-quote-cost-estimation-design.md`, đã chốt với chủ dự án): giá vốn/dòng chọn từ bảng giá NCC + %LN/dòng (legacy `percent_loi_nhuan_khach`) → giá bán; 3 hạng khách NL/TE/TN (legacy `percent_price_tre_em/tre_nho`); chi phí đoàn vs theo khách; tổng vốn/bán/lãi (`loi_nhuan_du_kien`) — công thức 1 chỗ `QuoteMath`.
- ✅ **Chuyển báo giá → đơn** (legacy `DuyetBooking`): quote Chấp nhận + có KH thật → đặt chỗ qua flow chuẩn (giữ chống overbooking, NL/TE/TN → hạng chỗ), doanh thu đơn = giá chốt báo giá, dòng KS/xe/visa/vé/vé bay sinh `ServiceBooking` (SL theo phạm vi, giá = giá vốn phải trả NCC), idempotent (`ConvertedOrderId`). FE nút "Chuyển đơn" + chọn chuyến.
- ✅ **Bản in báo giá** (`/quotes/:id/print`, thay legacy template .docx): mẫu thích ứng — có số khách in bảng giá 3 hạng (kiểu GroupTour), không có in bảng dòng đơn thuần (kiểu SingleTour); **chỉ hiện giá bán** (giá vốn/%LN/lãi là số nội bộ, không in); nút In/Xuất PDF (print dialog), trang sạch ngoài AppShell.
- ✅ **BillPaymentRequest** — đối chiếu legacy (`EditBillPaymentRequest` tạo PaymentVoucher + người duyệt): nghiệp vụ này hệ mới ĐÃ CÓ đầy đủ (Receipt/phiếu thu + ReceiptApproval nhiều cấp + PaymentVoucher + PaymentApproval). Chỉ bổ sung mắt xích UX: báo giá đã chuyển đơn → nút "Đơn" mở chi tiết đơn để lập phiếu thu; nút "Chuyển đơn" ẩn sau khi đã chuyển. **→ CỤM BAOGIA/DUTRU HOÀN TẤT** (dự trù giá → báo giá → in gửi khách → chuyển đơn → thu tiền/duyệt).
- ◻️ (Cần requirement) **Tour lẻ/FIT (`SingleTour`)** · **Đặt hotel/vé/visa nâng cao** — cần chốt tiếp với chủ dự án.
- ✅ (Follow-up) liên kết `OrderCost.ProviderServiceId` → chọn giá từ bảng giá NCC thay vì gõ tay (validate bảng giá thuộc đúng NCC; UI gợi ý sẵn tên dịch vụ + chi phí từ giá hợp đồng).

## Đợt 6 — Điều hành tour (Operations) 🚌

- **Quản lý HDV (`TourGuideManagement`)**: ngôn ngữ, lịch rảnh, ký giờ đi/về, quỹ vé ứng.
- **Quản lý xe (`CarManagement`)**.
- **Lịch điều hành (`Calendar`)**.

## Đợt 7 — Hạ tầng & tích hợp ⚙️ (làm khi cần)

`BackgroundJobs` (job nền: nhắc hạn giữ chỗ, gửi chăm sóc tự động) · `LogSystem` (audit) · `Upload` (đính kèm file) · `BankHub` (đối soát ngân hàng).

- ✅ **Hạ tầng Hangfire** (in-memory dev, dashboard `/hangfire`, guard testhost) + **Email** (`IEmailSender`: LogEmailSender dev / SmtpEmailSender prod qua `Email:Provider`).
- ✅ **`LogSystem`** = ActivityLog interceptor (audit tự động) · **`Upload`** = IFileStorage (local dev, tách theo tenant).
- ✅ **Job gửi chăm sóc tự động** (`CareReminderJob`): quét CustomerCare tới hạn nhắc → email người phụ trách; đa-tenant đúng cách (IgnoreQueryFilters + xử lý per-tenant). Dev log, prod SMTP.
- ✅ **Job nhắc hạn giữ chỗ** (`HoldReminderJob`): chỗ đang giữ (chưa cọc/chưa huỷ, `Seat.HoldExpiresAt`) còn ≤24h → email sales phụ trách đơn; idempotent (`HoldReminderSentAt`); per-tenant như CareReminderJob.
- ◻️ `BankHub` (cần API ngân hàng).

---

## Đề xuất mở rộng cấu trúc dữ liệu (bám hệ cũ)

Khi làm các đợt trên, bổ sung field còn thiếu ở entity hiện có:
- `Customer`: CustomerType, Source, Tag, TempBalance (tạm ứng), thông tin passport/visa.
- `Provider`: bảng giá dịch vụ theo hợp đồng (`provider_service_pricing`).
- `OrderCost` → tách/nâng thành liên kết với `ServiceManager` + `PaymentVoucher`.

## Ghi chú
- **Không cần làm hết 53 module** — nhiều module lẻ (`Pancake`, `BankHub`, `BillMiFi`, `ZaloUID`) là tích hợp phụ, làm khi có nhu cầu thật.
- Multi-tenant + Billing là phần **thêm mới của bản SaaS**, giữ nguyên.
- Mỗi đợt: viết plan chi tiết (bite-sized, TDD) riêng trước khi code.
