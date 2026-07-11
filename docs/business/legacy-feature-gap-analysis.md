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
3. ✅ **PhongBan (Department) + Position** (ĐÃ LÀM): cơ cấu tổ chức + `User.DepartmentId/PositionId` (nullable, additive — impact analysis báo CRITICAL 394 symbol nhưng 89 integration test auth/provisioning vẫn xanh vì chỉ thêm cột). Trang Users (list + gán inline phòng ban/chức vụ), perm `user.*` nhóm Admin. UserAdminService chỉ đọc + gán (không tạo/xoá user).
4. **LoaiDonHang / TrangThaiDonHang** → catalog loại đơn + trạng thái đơn (hiện `Order.Status` là int trần — mirror kiểu CustomerType keyed Code).
5. ✅ **CarType** (ĐÃ LÀM): catalog loại xe keyed theo số ghế (Code) → tên "Xe N chỗ", khớp Vehicle.SeatType. Dùng quyền `vehicle.*`.
6. ✅ **LanguagesType** (ĐÃ LÀM): catalog ngôn ngữ HDV (tên + mã ISO), dùng quyền `guide.*`. Chuẩn bị cho gán ngôn ngữ vào TourGuideAssignment sau.
7. ✅ **ExchangeRate → Currency** (ĐÃ LÀM): danh mục tỷ giá (Code/RateToVnd). ProviderService += CurrencyCode → giá vốn NCC nhập ngoại tệ, ProviderServiceService quy đổi VND tập trung (ContractPriceVnd/PublicPriceVnd trong DTO); picker OrderCost/Quote dùng giá VND (khoá tại thời điểm chọn). Quyền `service.*`.
8. ✅ **ConfigSurcharge / SurchargeServices** (ĐÃ LÀM): danh mục loại phụ thu (Fixed/Percent) + dòng phụ thu theo đơn cộng thẳng vào `Order.TotalRevenue` (tự chảy vào công nợ/hoa hồng/báo cáo, không sửa downstream). Bất biến `TotalRevenue = gốc + Σ phụ thu` → % luôn tính trên gốc bất kể thứ tự. `OrderMath.SurchargeAmount` một chỗ; panel trên chi tiết đơn + catalog. Quyền `booking.*`.
9. ✅ **ServicePaymentTerm → PaymentTerm** (ĐÃ LÀM): catalog điều khoản thanh toán NCC (tên + mô tả) + Provider.PaymentTermId (nullable). Reference cho đội điều hành khi lên lịch trả NCC. Quyền `provider.*`.
10. **age_type** → đã xử lý ngầm (NL/TE/TN trong báo giá) — chỉ cần nếu muốn cấu hình bậc tuổi động.

> Mẫu triển khai chuẩn cho catalog: xem `CustomerType`/`CustomerSource` (entity Shared + config unique index + service Catalog + controller + permission + test + frontend feature). ~30 phút/catalog, thuần additive.

---

## 🟠 Cần requirement/quyết định trước khi làm

- ✅ **Chuyển tour/đổi lịch** (`TransferHistory`/`CustomerHistoryChangeTour`) **ĐÃ LÀM** (chốt nghiệp vụ chuyên gia: đổi lịch GIỮ NGUYÊN giá/doanh thu): `POST /orders/{id}/transfers` dời đơn + toàn bộ chỗ sang chuyến đích, kiểm sức chứa (tái dùng guard overbooking), chặn chuyến đã đóng, ghi lịch sử + lý do. Chênh giá/cọc xử lý qua chi phí/phụ thu (ngoài thao tác đổi lịch). Panel + lịch sử trên chi tiết đơn. `ReasonSwitch`/`DetailReasonSwitch` (lý do chuẩn hoá) — làm khi cần.
- **Hotel/vé/visa chi tiết** (`class_hotel`, `hotel_type`, `ScanPassportHistory`, `VisaTourGuide`): đặt phòng theo hạng, hộ chiếu từng khách — đã ghi ở roadmap Đợt 5.
- **HDV nâng cao** (`RevenueExpensesInTourGuide`, `HandoverNote`, ký giờ): thu-chi hộ, bàn giao, ký xác nhận.
- **contract_tour** (hợp đồng tour — cần chốt mẫu HĐ). ✅ **BatchCreateTour ĐÃ LÀM**: `POST /tour-departures/batch` mở hàng loạt chuyến từ 1 mẫu (mỗi ngày → 1 chuyến Code=Prefix-STT, kế thừa mẫu); FE nút "Mở hàng loạt" sinh ngày định kỳ (số chuyến × khoảng cách ngày). (Phụ thu đã làm — xem trên.)

## 🔴 Tích hợp ngoài / subsystem lớn (làm khi có nhu cầu + API)

- **Gửi thật đa kênh**: ✅ **CẢ 3 KÊNH (Email/SMS/Zalo) đã nối qua abstraction** — CampaignService gửi qua `IEmailSender`/`ISmsSender`/`IZaloSender`, mỗi kênh dev ghi log (chạy ngay) / prod drop-in provider thật đọc `{Email,Sms,Zalo}:Provider`; bền per-recipient (lỗi → Status=Failed, không chặn). Email đã có SMTP thật. Còn lại chỉ là **implementation provider prod cụ thể** (SMS: Twilio/eSMS; Zalo: OA API) — cần API key + chọn nhà cung cấp; thêm 1 class không sửa code gọi.
- 🟡 **Notification in-app** (`Notification`/`NotificationOfEachUser`) ✅ **ĐÃ LÀM** (groundable, KHÔNG cần API ngoài): thông báo cá nhân + chuông đếm chưa đọc ở header + đánh dấu đã đọc/tất cả. Nối Tasking→Notification (giao việc → thông báo người nhận). Chỉ cần đăng nhập (thông báo cá nhân, lọc theo user hiện tại).
- 🟡 **Tasking** (`Tasking`/`UserInTasks`) ✅ **ĐÃ LÀM** (WorkTask — giao/theo dõi việc, lọc người+trạng thái, quyền `task.*`). ✅ **KPI** (`KeyPerformanceIndicator`) **ĐÃ LÀM** — `/reports/kpi` phễu báo giá→chấp nhận→chuyển đơn→thu tiền từ dữ liệu sẵn (`OrderMath.Rate` chia-0 an toàn). Còn `Workflow`/`SectionWork` (quy trình động) — cần thiết kế.
- 🟡 **CMS bài viết** (`Posts`/`CategoriesPost`) ✅ **ĐÃ LÀM** (groundable, KHÔNG cần external): Post + PostCategory (tiêu đề/slug/tóm tắt/nội dung/chuyên mục/nháp-xuất bản, PublishedAt tự set khi publish, slug duy nhất/tenant), lọc theo chuyên mục/trạng thái, quyền `post.*` (nhóm Content). Còn `CommentsPost`/`Likes` (tương tác công khai) — làm khi có cổng public.
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
| Cơ cấu tổ chức (Department/Position + Users) | PhongBan/Position | `feat(admin): cơ cấu tổ chức` |
| Phụ thu theo đơn (Surcharge/OrderSurcharge) | ConfigSurcharge/SurchargeServices | `feat(booking): phụ thu theo đơn` |
| Báo cáo doanh thu theo phòng ban | (dùng Department) | `feat(reports): doanh thu theo phòng ban` |
| Tỷ giá + giá vốn NCC ngoại tệ (Currency) | ExchangeRate | `feat(catalog): tỷ giá + giá vốn ngoại tệ` |
| Điều khoản TT NCC (PaymentTerm + Provider link) | ServicePaymentTerm | `feat(catalog): điều khoản thanh toán NCC` |

> **🟡 GROUNDABLE ĐÃ CẠN HẾT** (2026-07-11). Còn lại chỉ nhóm 🔴 (SMS/Zalo/Email gateway, Tasking/Workflow/KPI, CMS/blog, BankHub) — đều cần **API/credential ngoài** hoặc là subsystem lớn ngoài phạm vi điều hành tour.

**Ghi chú:** LoaiDonHang/TrangThaiDonHang **bỏ qua** — hệ mới đã model `Order.Status`/`Order.BookingType` bằng enum (tốt hơn bảng trạng thái legacy), không mirror redundant.

**Đề xuất làm tiếp (theo ưu tiên 🟡):** Department/Position (cơ cấu tổ chức, đụng `User` — chạy impact analysis trước) → ExchangeRate/ConfigSurcharge/ServicePaymentTerm (cần chốt điểm dùng). Các catalog thuần đã phủ hết. Mỗi cái mirror mẫu catalog có sẵn, thuần additive, ~1 commit. Ngoài ra: in tài khoản mặc định lên bản in báo giá (nối PaymentAccount vào QuotePrintPage).
