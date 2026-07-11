# Phân tích gap tính năng: hệ cũ (127 bảng) ↔ hệ mới

> Nguồn: trích 127 bảng `CREATE TABLE` từ `script.sql` (schema hệ cũ, UTF-16), đối chiếu với ~52 entity hệ mới.
> Cập nhật: 2026-07-11. Đây là **bản đồ đầy đủ** để quyết định làm tiếp — bổ sung cho `legacy-gap-roadmap.md` (roadmap theo đợt).

## Tổng quan

| Nhóm trạng thái | Số bảng (xấp xỉ) | Ý nghĩa |
|---|---|---|
| ✅ Đã có | ~45 | Có entity/luồng tương đương hệ mới |
| 🟡 Groundable — nên làm | ~12 | Mirror được từ legacy, thuần additive, không cần quyết định sản phẩm |
| 🟠 Cần requirement/quyết định | ~15 | Groundable nhưng nghiệp vụ phải chốt (chuyển tour, hotel chi tiết, surcharge…) |
| 🔴 Tích hợp ngoài / subsystem lớn | ~25 | SMS/Zalo/Email gateway, Tasking/Workflow/KPI, CMS, BankHub… |
| ⚪ Hạ tầng/hệ thống — bỏ qua | ~30 | `__MigrationHistory`, `Temps`, `Loggers`, `SettingPage`, config nội bộ… |

---

## ✅ Đã có (map legacy → hệ mới)

| Legacy | Hệ mới |
|---|---|
| customers, customer_type, customer_source | Customer, **CustomerType** (mới), **CustomerSource** (mới) |
| Customer_Care, Rate, LeadCustomers | CustomerCare, TourRating, Lead |
| tours, tour_samples, tour_itineraries, tour_customers, tour_sellers | Tour (+template), TourItinerary, TourCustomer, TourAssignee |
| PriceScenario, MarketType | PriceScenario, MarketType |
| Orders, Order_Chi, CancelSeats | Order, OrderCost, CancelSeat |
| N_ReceiptVoucher, N_PaymentVoucher | ReceiptVoucher, PaymentVoucher |
| ApprovalProcess/Step/StepUser, ReceiptVoucherApproval | ReceiptApproval + PaymentApproval (engine duyệt nhiều cấp) |
| Comission, CommissionCampaign, ProfitSharing | CommissionRule, CustomerCommissionRule, ProfitShare |
| providers, provider_services, provider_service_pricing, Provider_Service_Orders, services | Provider, ProviderService, ServiceBooking, ServiceItem |
| vehicle, TourGuide, TicketFund, Calendar | Vehicle, TourGuideAssignment, **VehicleAssignment** (mới), TicketFund, OperationsCalendar (UI) |
| Marketing_Campain, Marketing_LichSuGui | MarketingCampaign, MarketingSendLog |
| Permission, GroupPermission, FunctionQuyen/SubjectQuyen, users | RBAC: Permission/Role/RolePermission/UserRole, User |
| FileUploads, ActivityLogs, BackgroundJobs | FileUpload, ActivityLog, Hangfire jobs |

---

## 🟡 Groundable — nên làm (ưu tiên giảm dần)

1. **customer_tag / Tags / TagMappings** → `CustomerTag` catalog (m-n) hoặc giữ `Customer.Tag` string + catalog chuẩn hoá. Bổ sung màu (legacy Tags.color). *Cùng cụm phân loại khách vừa làm.*
2. ✅ **PaymentMethod → `PaymentAccount`** (ĐÃ LÀM): catalog tài khoản nhận tiền (tên hiển thị, ngân hàng, số TK, chủ TK, chi nhánh, nội dung CK mặc định) để in báo giá/hoá đơn. Quy tắc: 1 tài khoản `IsDefault`/tenant. Permission `paymentaccount.*` (nhóm Finance). *Còn lại: in tài khoản mặc định lên bản in báo giá — làm khi nối.*
3. **PhongBan (Department) + Position + User_PhongBan** → cơ cấu tổ chức; gắn `User.DepartmentId/PositionId` (đụng User — chạy impact trước). Phục vụ báo cáo theo phòng ban.
4. **LoaiDonHang / TrangThaiDonHang** → catalog loại đơn + trạng thái đơn (hiện `Order.Status` là int trần — mirror kiểu CustomerType keyed Code).
5. ✅ **CarType** (ĐÃ LÀM): catalog loại xe keyed theo số ghế (Code) → tên "Xe N chỗ", khớp Vehicle.SeatType. Dùng quyền `vehicle.*`.
6. ✅ **LanguagesType** (ĐÃ LÀM): catalog ngôn ngữ HDV (tên + mã ISO), dùng quyền `guide.*`. Chuẩn bị cho gán ngôn ngữ vào TourGuideAssignment sau.
7. **ExchangeRate** → tỷ giá (đa tiền tệ) — cần chốt điểm dùng (giá vốn NCC ngoại tệ?).
8. **ConfigSurcharge / SurchargeServices** → phụ thu — cần chốt cách áp vào đơn.
9. **ServicePaymentTerm** → điều khoản thanh toán NCC.
10. **age_type** → đã xử lý ngầm (NL/TE/TN trong báo giá) — chỉ cần nếu muốn cấu hình bậc tuổi động.

> Mẫu triển khai chuẩn cho catalog: xem `CustomerType`/`CustomerSource` (entity Shared + config unique index + service Catalog + controller + permission + test + frontend feature). ~30 phút/catalog, thuần additive.

---

## 🟠 Cần requirement/quyết định trước khi làm

- **Chuyển tour** (`TransferHistory`, `ReasonSwitch`, `DetailReasonSwitch`, `HistoryDetailReasonSwitchs`, `CustomerHistoryChangeTour`): dời khách sang chuyến khác + lý do + lịch sử. Groundable nhưng cần chốt nghiệp vụ (giữ tiền/cọc, chênh giá).
- **Hotel/vé/visa chi tiết** (`class_hotel`, `hotel_type`, `ScanPassportHistory`, `VisaTourGuide`): đặt phòng theo hạng, hộ chiếu từng khách — đã ghi ở roadmap Đợt 5.
- **HDV nâng cao** (`RevenueExpensesInTourGuide`, `HandoverNote`, ký giờ): thu-chi hộ, bàn giao, ký xác nhận.
- **Surcharge/phụ thu**, **contract_tour** (hợp đồng tour), **BatchCreateTour** (tạo hàng loạt chuyến).

## 🔴 Tích hợp ngoài / subsystem lớn (làm khi có nhu cầu + API)

- **Gửi thật đa kênh**: `SMS`/`SMS_Campaign`/`Send_Sms_History` (SMS gateway), `Email_*`/`MailHistory` (email campaign — hạ tầng IEmailSender đã có, thiếu campaign UI), `ZaloCampain`/`ZaloUID`/`ZaloZNS` (Zalo OA).
- **Notification** in-app (`Notification`/`NotificationOfEachUser`/`NotifiInUser`).
- **Tasking/Workflow/KPI** (`Tasking`/`UserInTasks`/`Workflow`/`KeyPerformanceIndicator`/`SectionWork`) — subsystem quản trị công việc.
- **CMS/blog** (`Posts`/`CategoriesPost`/`CommentsPost`/`Likes`) — ngoài phạm vi điều hành tour.
- **BankHub** (`BankHub`/`APIKeyMifi`) — đối soát ngân hàng, cần API.
- **BookingTickets** — hệ ticket hỗ trợ nội bộ.

## ⚪ Hạ tầng/hệ thống — không port

`__MigrationHistory`, `Temps`, `ExecutedFiles`, `Loggers`, `SystemSptValue`, `user_grid_settings`, `Config`/`config_company`/`config_contract`/`SettingPage` (thay bằng cấu hình hệ mới), `ActivityLogsNewVersion` (đã có ActivityLog).

---

## Đã triển khai trong phiên phân tích này

| Feature | Legacy | Commit |
|---|---|---|
| Phân xe cho chuyến (VehicleAssignment) | (điều hành, song song TourGuide) | `feat(operations): phân xe cho chuyến` |
| Danh mục loại khách (CustomerType) | customer_type | `feat(catalog): danh mục loại khách` |
| Danh mục nguồn khách (CustomerSource) | customer_source | `feat(catalog): danh mục nguồn khách` |
| Danh mục nhãn khách (CustomerTag) | Tags/customer_tag | `feat(catalog): danh mục nhãn khách` |
| Tài khoản nhận tiền (PaymentAccount) | PaymentMethod | `feat(catalog): tài khoản nhận tiền` |
| In TK mặc định lên báo giá | (nối PaymentAccount) | `feat(quotes): in tài khoản nhận tiền` |
| Loại xe (CarType) | CarType | `feat(catalog): loại xe` |
| Ngôn ngữ HDV (LanguageType) | LanguagesType | `feat(catalog): ngôn ngữ HDV` |

**Ghi chú:** LoaiDonHang/TrangThaiDonHang **bỏ qua** — hệ mới đã model `Order.Status`/`Order.BookingType` bằng enum (tốt hơn bảng trạng thái legacy), không mirror redundant.

**Đề xuất làm tiếp (theo ưu tiên 🟡):** Department/Position (cơ cấu tổ chức, đụng `User` — chạy impact analysis trước) → ExchangeRate/ConfigSurcharge/ServicePaymentTerm (cần chốt điểm dùng). Các catalog thuần đã phủ hết. Mỗi cái mirror mẫu catalog có sẵn, thuần additive, ~1 commit. Ngoài ra: in tài khoản mặc định lên bản in báo giá (nối PaymentAccount vào QuotePrintPage).
