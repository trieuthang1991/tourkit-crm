# Roadmap bù khoảng trống so với hệ cũ (KojiCRM/TourKit legacy)

> Nguồn: đối chiếu 53 module hệ cũ (`CMS/KojiCRM/Modules/`) với hệ mới. Hệ mới hiện phủ **lõi đặt tour → thu tiền → duyệt** (~11/53 module). Roadmap này phân đợt theo **giá trị vận hành / mức chặn**, không nhất thiết làm hết 53 module.

Nguyên tắc: **usable-first** — vá thứ chặn vận hành trước, mở rộng phạm vi sau. Mỗi đợt ra được phần mềm chạy + test xanh, commit từng task.

---

## Đợt 0 — Vá lỗ hổng chặn vận hành (nhỏ, gấp) ⚡

Thứ đã có gần đủ nhưng thiếu mảnh khiến không dùng thật được.

| # | Việc | Vì sao gấp | Quy mô |
|---|---|---|---|
| 0.1 | **Chống overbooking** khi đặt/giữ chỗ | Hiện đặt vượt `TotalSlots` vẫn thành công (đã xác minh). Hệ cũ có `sp_getapplock` | Nhỏ — thêm kiểm tra tổng chỗ đã dùng + concurrency (RowVersion/transaction) trong `BookingFactory` |
| 0.2 | **Đóng chuyến** (close departure) | Cột `IsClosed/ClosedAt` là cột chết, chưa có handler → chuyến không chốt sổ được | Nhỏ — endpoint `POST /tour-departures/{id}/close` + guard chặn thao tác sau khi đóng |
| 0.3 | **MarketType** hoàn thiện CRUD | Mới có GET/POST, thiếu Update/Delete + chưa qua dispatcher (lệch kiến trúc) | Nhỏ |
| 0.4 | **CreateDeparture copy giá/itinerary từ template** | Hệ cũ mở chuyến kế thừa cấu hình mẫu; mới thì chuyến trống | Nhỏ–vừa |

---

## Đợt 1 — Tài chính chi (Payment) 💸

Công ty tour không vận hành được nếu chỉ có thu mà không có chi.

- **Phiếu chi (`PaymentVouchers`)**: entity riêng (Status/PaymentMethod/Partner/duyệt chi), tách khỏi `OrderCost`. Bám `N_PaymentVoucher` hệ cũ.
- **Duyệt chi**: tái dùng state machine `Workflow`/approval đã có cho phiếu thu.
- **Công nợ phải trả NCC**: report + số dư theo provider (đối xứng công nợ phải thu).
- (Sau) **Dự trù chi phí tour (`DuTruTours`)**: bảng dự trù trước khi vận hành chuyến.

## Đợt 2 — Báo cáo & Dashboard 📊

Hệ cũ có ~19 report; hệ mới mới có 1 (công nợ phải thu). Ưu tiên báo cáo quản trị hằng ngày:

- Doanh thu (theo thời gian / thị trường / loại tour).
- Dòng tiền theo phương thức thanh toán.
- Lợi nhuận & hoa hồng theo nhân viên.
- Công nợ phải trả NCC (nối Đợt 1).
- Dashboard trang chủ (`HomePage`/`WorkSpaceView`) + KPI cơ bản.

## Đợt 3 — Hoa hồng sales đúng nghĩa 🎯

Tách bạch với `ProfitShare` (chia lợi nhuận) hiện tại:

- **Hoa hồng theo bậc/loại khách/chiến dịch** (`ConfigCommission`, `CommissionCampaign`, `CommissionProfitLevel`).
- **Chốt sổ hoa hồng** tách khỏi chốt đơn (`StatusComission`, `date_closed`).
- Đổi tên/nhãn để không nhầm "Commission" (đang là chia lợi nhuận) với hoa hồng.

## Đợt 4 — CSKH sau tour & CRM sâu 🤝

- **CSKH (`CustomerCare`)**: lịch chăm sóc, ghi nhận tương tác.
- **Đánh giá/feedback sau tour (`FeedBackTour`)**.
- **Gửi thật** Email/SMS/Zalo (hiện chỉ log) — nối provider gửi.
- (Tuỳ nhu cầu) `Tasking` (giao việc), `Notification`, `CallCenter`, `Pancake`.

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
