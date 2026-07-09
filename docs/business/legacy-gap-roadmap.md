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

## Đợt 5 — Dịch vụ lẻ (không chỉ tour trọn gói) 🧳

Hệ cũ đặt **cả dịch vụ lẻ**; hệ mới mới có tour trọn gói.

- **Báo giá (`BaoGia`)** — bước trước khi chốt booking.
- **Tour lẻ / FIT (`SingleTour`)** cạnh tour đoàn (`GroupTour`).
- **Catalog dịch vụ (`ServiceManager`)** + bảng giá NCC theo hợp đồng (`provider_service_pricing`).
- **Đặt khách sạn / vé máy bay / visa (`BookingHotel`, `AirPlaneTicket`, `Visa`, `BookingTicket`)**.
- **Hoá đơn VAT (`InvoiceBranch`)**.

## Đợt 6 — Điều hành tour (Operations) 🚌

- **Quản lý HDV (`TourGuideManagement`)**: ngôn ngữ, lịch rảnh, ký giờ đi/về, quỹ vé ứng.
- **Quản lý xe (`CarManagement`)**.
- **Lịch điều hành (`Calendar`)**.

## Đợt 7 — Hạ tầng & tích hợp ⚙️ (làm khi cần)

`BackgroundJobs` (job nền: nhắc hạn giữ chỗ, gửi chăm sóc tự động) · `LogSystem` (audit) · `Upload` (đính kèm file) · `BankHub` (đối soát ngân hàng).

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
