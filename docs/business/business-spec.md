# TourKit — Đặc tả nghiệp vụ (Business Specification)

> Nguồn: phân tích hệ gốc `migroup-vn/tourkit` (124 entity, 50 module). Tái cấu trúc để chạy mô hình SaaS đa doanh nghiệp lữ hành.
> Cập nhật: 2026-07-07.

Mục tiêu: mô tả **logic nghiệp vụ** (không phải kỹ thuật) đủ chi tiết để thiết kế schema và lập kế hoạch code. Mỗi khối nêu: *đối tượng* → *thuộc tính cốt lõi* → *quan hệ* → *luồng nghiệp vụ chính*.

---

## 0. Mô hình tổng thể

TourKit là hệ điều hành tour cho **công ty lữ hành** (B2B). Một doanh nghiệp = 1 **tenant**. Mỗi tenant có dữ liệu cô lập hoàn toàn (khách, tour, đơn, nhân sự, tài chính riêng) nhưng dùng chung codebase + database (shared DB + `TenantId`).

**Chuỗi giá trị cốt lõi:**
```
Khách (Customer/Lead)
  → Báo giá / Mẫu tour (TourSample + PriceScenario)
  → Đơn/ vé (Order + BookingTicket + TourCustomer)
  → Mở chuyến cụ thể (Tour) + điều phối (Provider, TourGuide)
  → Thu/Chi (Receipt/PaymentVoucher + Approval workflow)
  → Chốt hoa hồng (Commission/ProfitSharing) + chốt chuyến
```

Các mảng phụ trợ: Marketing đa kênh (Email/SMS/Zalo), Task/Nhật ký, Báo cáo.

**Phân loại nhân sự trong tenant** (mỗi user thuộc phòng ban + có vị trí):
- **Sales** — bán tour, chịu trách nhiệm khách + đơn.
- **Operator/Điều hành** — mở chuyến, gán NCC, sắp lịch.
- **Kế toán** — phiếu thu/chi, công nợ.
- **Quản lý** — duyệt (approval), phân quyền.

---

## 1. Khách hàng (Customer)

### 1.1 Phân loại 2 lớp
- **LeadCustomer** — khách tiềm năng (chưa giao dịch). Có trạng thái lead, được gán cho sales theo cơ chế **round-robin** (`SalesAssign`, `Rotation_Order`, `SaleEquallyDivine`, `AutoScale`). Đây là bộ phận **phân bổ lead tự động**.
- **Customer** — khách thật, sinh khi Lead "thành công" hoặc nhập trực tiếp. Trỏ lại `LeadCustomerId` để truy nguồn.

### 1.2 Thuộc tính Customer (cốt lõi)
- Danh tính: `full_name`, `first_name`/`last_name`, `gender`, `birthday`, `address`, `phone_number`, `email`.
- Pháp lý/tax: `id_type`, `id_number`, `customer_tax_code`, `customer_unit_name` (khách công ty).
- Mã hóa: `customer_code` (mã nội bộ, duy nhất/tenant).
- Phân loại: `customer_type_id`, `initial_customer_type_id` (loại ban đầu để so sánh), `customer_source_id`, `customer_group_id`, `marketId` (thị trường), `tagCustomer` (tag).
- Trạng thái: `status`, `trash_status` (thùng rác — soft-delete 2 cấp), `temp_balance` (công nợ tạm).
- Theo dõi: `IdsNguoiTheoDoi` (danh sách user theo dõi, CSV — anti-pattern cần normalize), `ctv` (cộng tác viên).
- Audit: `INS_DTTM/INS_UID/UPD_DTTM/UPD_UID/AUD_VER` (đánh dấu phiên bản — optimistic locking).

### 1.3 Quan hệ
- `CustomerSource` (nguồn: FB, web, giới thiệu…), `CustomerType` (loại: VIP, thường…), `CustomerTag`/`Tags`/`TagMappings` (tag nhiều-nhiều).
- `CustomerCare` — mỗi dòng là 1 hoạt động chăm sóc (gọi, gặp, email…), có `Care_Title`, `Care_End_Time`, `status`, `Feedback`, `TimeCareRemind` (nhắc), `TypeSchedule` (lịch định kỳ).
- Lịch sử đi tour: `TourCustomer` (xem §4).
- Lịch sử bình luận: `CommentTour`.
- Chuyển đổi: Lead → Customer khi có giao dịch đầu tiên.

### 1.4 Luồng nghiệp vụ
1. Thu nạp Lead (từ marketing/manual) → phân bổ sales tự động.
2. Sales chăm sóc (CustomerCare, có nhắc lịch).
3. Lead "chốt" → tạo Customer + liên kết ngược.
4. Khách đặt tour → sinh Order/TourCustomer.
5. Hệ thống theo dõi: lịch đi tour, công nợ, loại khách (nâng/hạ loại dựa giao dịch).

---

## 2. Mẫu tour & Báo giá (Tour TPT + Pricing)

> **Lưu ý thiết kế (cập nhật):** Hệ cũ tách `tour_samples` + `tours` thành 2 bảng. SaaS
> mới **gộp theo pattern TPT** (Table-per-Type): 1 bảng gốc `Tour` (cột chung: Code,
> Title, ngày, trạng thái...) + 2 bảng phụ 1-1 theo `Kind` (Template | Departure).
> `TourCustomer` vẫn bảng riêng (quan hệ 1:N). Xem chi tiết schema + ERD ở
> `database-optimization-analysis.md` §B3, §F, §G.

Vẫn tách biệt rõ 2 khái niệm **cốt lõi** để tránh nhầm lẫn:

| | TourTemplate (mẫu, Kind=Template) | TourDeparture (chuyến, Kind=Departure) |
|---|---|---|
| Bản chất | Sản phẩm bán lại nhiều lần | Một lần khởi hành cụ thể |
| Số lượng | Không giới hạn | Cố định, theo slot |
| Mục đích | Catalog + báo giá | Điều hành + chốt doanh thu |
| Schema | bảng gốc `Tour` + phụ `TourTemplateFields` | bảng gốc `Tour` + phụ `TourDepartureFields` |

### 2.1 TourSample — sản phẩm catalog
- Danh tính: `tour_code`, `title`, `tour_type` (inbound/outbound/domestic…), `typeOf` (loại hình).
- Thời gian: `departure_date`, `end_date`, `booking_date`, `Reversation_Time` (thời hạn giữ chỗ).
- Sức chứa: `numer_of_slots` (tổng slot), `oversold_seats` (cho phép oversell).
- Địa điểm: `place_pickup`/`place_pickdown`, điểm đón/trả xe.
- Vận chuyển: `transportation` (đi lại: máy bay/xe/tàu).
- Lịch trình: `flight_itinerary`, `linkItinerarys`, `TourItinerary` (chi tiết ngày).
- Ghi chú điều khoản: `TermsNote`, `TermsNoteEnglish` (VN/EN song song).

### 2.2 Giá — đa tầng theo độ tuổi + kịch bản
Đây là phần **phức tạp nhất**. TourSample lưu giá theo 4 nhóm tuổi:
- `price_per_slot` / `commission_adults` (+ `type_commission_adults`: % hoặc số tiền).
- `price_childs` / `commission_childs`.
- `price_childs_small` / `commission_childs_small`.
- `price_baby` / `commission_baby`.

**PriceScenario** — kịch bản giá biến đổi:
- `NumberFrom`/`NumberTo` (khoảng số lượng khách) → `MoneyScenario` (giá khác theo đoàn đông/thiểu).
- VD: đoàn 10-15 người giá 5tr, 16-20 người giá 4.5tr.

### 2.3 TourItinerary — chương trình ngày
- `tour_id`, `title`, `detail`, `IndexDay` (thứ tự ngày), `TitleDay` (tiêu đề ngày).
- TourType phân biệt mẫu vs chuyến.

**Luồng báo giá:** Sales chọn TourSample → nhập số khách → hệ tính giá theo PriceScenario + nhóm tuổi → in báo giá (VN/EN). Khi chốt, tạo Tour (chuyến cụ thể) + Order.

---

## 3. Chuyến cụ thể (Tour) + Điều hành

### 3.1 Tour — chuyến khởi hành
- Liên kết mẫu: (`parentId`, `QuoteSample`) — chuyến sinh từ mẫu.
- Thời gian: `departure_date`/`end_date`/`departure_time`/`end_time`.
- Số liệu: `amount_adults`, `amount_children`, `so_luong`.
- Tài chính snapshot: `tien_chua_phu_thu` (giá gốc trước phụ thu).
- Trách nhiệm: `NguoiPhuTrach` (người phụ trách), `ManagerIds`, `IdsNguoiTheoDoi`.
- Trạng thái nghiệp vụ: `status`, `StatusCloseTour` (chốt chuyến), `StatusComission` + `DateClosedComission` (chốt hoa hồng).
- Đặc thù: `ma_yeu_cau` (mã yêu cầu), `phan_thu` (loại thu), `IsGhiNhanDongTien` (ghi nhận đóng tiền).

### 3.2 TourGuide (hướng dẫn viên)
- Là **đặc biệt**: vừa là nhân sự nội bộ, vừa là NCC (đi tour).
- Entity `TourGuide` tách khỏi `User`: vì HDV có thể thuê ngoài.
- Thuộc tính: `Skill`, `Languages`, `Birthday`, `Gender`, `Frequency` (tần suất đi tour), `Avatar`.
- `VisaTourGuide` — visa cho HDV (cross-border).
- `RevenueExpensesInTourGuide` — doanh thu/chi phí của HDV theo tour.

### 3.3 Điều phối NCC
- Qua `OrderChi` (xem §5) — mỗi dịch vụ thuê NCC là một dòng chi.
- `CarType`, `Vehicle` — xe + loại xe.

---

## 4. Đặt chỗ & Vé (Booking)

Hệ cũ tách thành nhiều entity, **dễ nhầm**. Mô hình hóa lại:

### 4.1 Ba lớp đặt chỗ
1. **BookingTicket (phiếu đặt)** — yêu cầu đặt ban đầu, có thể từ website/agent.
   - `CodePhieu` (mã phiếu), `TenKH`, `SoDienThoaiKH`, `SoLuong`/`QuantityChild`/`QuantityBaby`, `Gia`/`GiaChild`/`GiaBaby`.
   - `TrangThaiPhieu` (trạng thái), `NguonPhieu` (nguồn), `IsConfirmed`/`ConfirmedAt`/`ConfirmedBy`.
   - Liên kết: `TourIdRoot`/`TourTypeRoot` (mẫu hay chuyến), `IdKhachHang` (khách có thể chưa là Customer).
   - `DetailReasonSwitch` — lý do chuyển/hủy phiếu.
2. **Order (đơn hàng)** — đơn chính thức sau khi xác nhận BookingTicket.
3. **TourCustomer (slot trên chuyến)** — từng khách ngồi trên chuyến cụ thể.

### 4.2 TourCustomer — chi tiết chỗ (đa giá)
Lưu giá + phí + chiết khấu **per-khách theo 4 nhóm tuổi**:
- Giá: `price_per_slot`, `price_childs`, `price_childs_small`, `price_baby`.
- Phụ thu: `surcharge`, `childs_surcharge`, `childs_surcharge_small`, `baby_surcharge`.
- Chiết khấu: `discount`, `discount_childs`, `discount_childs_small`, `discount_baby`.
- Hoa hồng: `comission`, `comission_childs`, `comission_childs_small`, `comission_baby`.
- Số lượng: `quantity`, `amount_children`, `amount_children_small`, `quantity_baby`.
- Khác: `booking_type`, `upfront_amount` (tiền ứng), `seat_selected` (chỗ ngồi), `reservation_code`, `IdBranch` (chi nhánh), `is_main_contact`, `signature` (ký nhận).

### 4.3 Đặt chỗ phụ
- **CancelSeat** — hủy ghế (có lý do, theo thời gian).
- **PendingOrder** — giữ chỗ tạm, chưa chốt.
- **ScanQuotaOrder** — quét quota (giới hạn slot cho agent).
- **BookingTicketComment** — bình luận trên phiếu.

### 4.4 Luồng đặt
```
Khách yêu cầu
  → BookingTicket (phiếu, trạng thái "chờ")
  → Confirm → tạo Order + TourCustomer (slot)
  → Có thể đổi/hủy (CancelSeat, DetailReasonSwitch)
  → Tour diễn ra → chốt
```

---

## 5. Đơn hàng & Chi phí (Order + OrderChi)

### 5.1 Order — đơn tổng
Là **giao dịch tài chính trung tâm**, gắn 1 khách ↔ 1 tour:
- Liên kết: `Tour_Id`, `Customer_Id`, `TourSample` (qua mẫu).
- **Doanh thu**: `Total_Thu_Money` (thu thực tế), `TotalRevenueRoot` (thu gốc), `ApprovedRevenue`/`UnapprovedRevenue` (duyệt/chưa), `ApprovedRevenueSingle` (đơn lẻ).
- **Chi phí**: `Total_Chi_Money` (chi tổng), `Total_Chi_Money_Sale` (chi sales), `Total_ChiDK_Money_Sale` (chi định khoản).
- **Hoàn**: `TotalRefund`.
- Trạng thái: `status`, `IsGhiNhanDongTien` (ghi nhận tiền).

> **Quyết định thiết kế:** GIỮ cột tổng doanh thu/chi phí (`Total_Thu_Money`, `ApprovedRevenue`…)
> TRỰC TIẾP trên `Order` — **denormalize có chủ đích**; chỉ tách **chi tiết dòng chi phí NCC** ra
> `OrderCost` (đổi tên từ `OrderChi`).
> *(Đây là đảo ngược đề xuất "tách OrderRevenue/OrderCost" ban đầu — lý do đầy đủ + cơ chế đồng bộ
> cột tổng xem `database-optimization-analysis.md` §A6 và §I.)*

### 5.2 OrderChi — chi tiết chi phí NCC
Mỗi dòng = 1 dịch vụ thuê từ 1 NCC trong 1 đơn:
- Liên kết: `Order_Id`, `provider_id`, `service_id`.
- Tiền: `total_chi` (chi phí), `coc` (cọc), `ExpectedCost` (chi phí dự kiến), `Surcharge`, `VAT`.
- Ngày: `IndexDay` (ngày thứ mấy trong tour), `TitleDay`, `date_signed`.
- Trạng thái: `status`, `check_email` (đã gửi mail NCC?).

### 5.3 Trạng thái đơn (TrangThaiDonHang + LoaiDonHang)
- `TrangThaiDonHang` — trạng thái theo *loại* đơn (mỗi loại có bộ trạng thái riêng).
- `LoaiDonHang` — loại đơn (tour trọn gói, chỉ vé, only land…).

---

## 6. Nhà cung cấp (Provider) + Công nợ

### 6.1 Provider — NCC đa loại
Cùng 1 entity phục vụ nhiều loại NCC:
- **Khách sạn**: `class_hotel_id` (sao), `ht_type_id`.
- **Xe**: `car_type` (thông qua `Vehicle`, `CarType`).
- **HDV**: (qua `TourGuide`).
- **Nhà hàng / vé máy bay / dịch vụ khác**.
- Liên hệ: `phone_contact`, `email_contact`, `contact_person_information`.
- Tài khoản ngân hàng: `BankUserName`, `BankUserAccount`, `BankName`, `tax_code`.
- Đánh giá: `rate` (xếp hạng chất lượng).

### 6.2 ProviderService — hợp đồng dịch vụ NCC
- `ProviderService` — hợp đồng theo dịch vụ.
- `ProviderServicePricing` — giá (theo mùa/số lượng).
- `ProviderServiceOrder` — đặt dịch vụ trong đơn.
- `ServicePaymentTerm` — điều khoản thanh toán.

### 6.3 Công nợ
- `AccountBalance` — số dư theo user/phương thức (`MoneyRemain`, `IdPaymentMethod`).
- `OrderProviderMoney` — tiền NCC theo đơn.

---

## 7. Tài chính (Thu/Chi + Approval)

### 7.1 Hai loại phiếu
- **ReceiptVoucher (phiếu thu)** — tiền khách nộp vào (NHẦM tên hệ cũ: thực ra là phiếu **chi cho NCC** — xem `Order_Chi_Id`, `Order_Provider_Money_Id`. **Phải đổi tên rõ ràng khi redesign**).
- **PaymentVoucher (phiếu chi)** — tiền chi ra.

> ⚠️ **Quan trọng**: Trong hệ cũ `ReceiptVoucher` lại có `LoaiPhieuChi`, `TrangThaiChi` → rất dễ nhầm. Khi redesign SaaS phải **đặt tên nhất quán theo dòng tiền thực**: tiền vào = `Receipt`, tiền ra = `Payment`.

Cả hai có cấu trúc gần giống:
- Mã: `Voucher_Code`, tiêu đề `Voucher_Title`, ngày `Voucher_Dttm`.
- Tiền: `Payment_Money`, `PrePayment` (ứng) / `PreReceipt`.
- Đối tác: `Partner`, `Receiver_Name`, `Address`, `Phone_Number`, `Email`.
- Ngân hàng: `BankUserName`, `BankUserNumber`, `BankName`.
- Phân loại: `LoaiPhieuTaiChinh` (loại), `IdPaymentMethod`, `LoaiPhieuThu`/`LoaiPhieuChi`.
- Trạng thái: `Status`, `StatusClose`, `IsGhiNhanDongTien`, `ParentId` (phiếu cha — tách phiếu), `TotalMoneyByChilds`.
- Duyệt: `IdUserSign`, `UserApprove_Department`, `ReceiptVoucherApprovalStepUser`.

### 7.2 Loại phiếu & phương thức
- `LoaiPhieuTaiChinh` — loại phiếu (thu tiền tour, thu cọc, chi NCC, chi nội bộ…).
- `PaymentMethod` — tiền mặt, chuyển khoản, thẻ, quỹ.

### 7.3 Workflow duyệt (Approval)
Hệ duyệt dùng chung, áp dụng cho phiếu thu/chi/đơn:
- `ApprovalProcess` — quy trình (`name`, `method` — kiểu duyệt: tuần tự/đồng thời/any).
- `ApprovalStep` — bước duyệt (`step_order`, `position_id` — vị trí duyệt).
- `ApprovalStepUser` — user cụ thể ở mỗi bước.
- `ApprovalProcessHistory` — lịch sử duyệt.
- Áp dụng: `ReceiptVoucherApproval` + `ReceiptVoucherApprovalStepUser`.

---

## 8. Hoa hồng & Chia lợi nhuận

Phức tạp, áp dụng cho **sales** và **người chia sẻ lợi nhuận**:

### 8.1 Commission (hoa hồng sales)
- `Comission`: `user_id`, `commission_percentage`, `id_customer_type` (hoa hồng khác theo loại khách).
- `CommissionCampaign` — chiến dịch hoa hồng (theo thời gian, có thể đè cấu hình).
- `CommissionProfitLevel` — hoa hồng theo cấp lợi nhuận (lợi nhuận càng cao, % càng lớn).
- `ConfigCommission` — cấu hình chung.

### 8.2 ProfitSharing (chia lợi nhuận)
- `UserId`, `TourId`, `TourType`, `Percentage` (% chia), `Comission` (tiền), `TotalRevenueByComission`.
- Áp dụng cho chuyến cụ thể: 1 tour có nhiều người chia lợi nhuận.

**Luồng chốt:** Tour xong → tính doanh thu − chi phí = lợi nhuận → tính hoa hồng sales → chia lợi nhuận → chốt (`StatusCloseTour`, `DateClosedComission`).

---

## 9. Nhân sự & Phân quyền

### 9.1 User + cơ cấu tổ chức
- `User` — nhân viên (rất nhiều trường nhân sự: sinh, CCCD, hôn nhân, dân tộc, tôn giáo, MST, tài khoản NH… → nhiều field HR thừa cho SaaS MVP, cần **cắt bỏ**).
- `Position` — vị trí (Sales, Operator, Kế toán, QL).
- `PhongBan` — phòng ban.
- `UserPhongBan` — user thuộc phòng ban nào (nhiều-nhiều).
- `BranchType` — chi nhánh, `GroupType` — nhóm, `MarketType` — thị trường (3 phân nhóm hoạt động).

### 9.2 Phân quyền (CẦN làm lại hoàn toàn)
Hệ cũ tự chế 5 bảng: `SubjectQuyen` → `FunctionQuyen` → `SubjectQuyenFunctionQuyen`, + `PhongBanSubjectQuyen`, `UserPermission`. Quá phức tạp, khó hiểu.

**SaaS redesign → RBAC chuẩn:**
- **Role** (vai trò): Admin, Manager, Sales, Operator, Accountant.
- **Permission** (quyền hạt): `tour.create`, `order.approve`, `finance.voucher.sign`…
- **RolePermission** (nhiều-nhiều), **UserRole** (nhiều-nhiều).
- Có thể thêm **phân quyền theo phòng ban/chi nhánh** (data scope).

---

## 10. Marketing & Truyền thông

Đa kênh gửi tin:
- **Email**: `EmailCampaign`/`EmailManager`/`EmailSample` + `SendEmailHistory`/`MailHistory`.
- **SMS**: `SMSCampaign`/`SMS` + `SendSmsHistory`.
- **Zalo**: `ZaloCampain`/`ZaloZNS`/`ZaloUID`.
- **Template**: `MarketingTemplate`, `MarketingCampain` + `MarketingLichSuGui` (lịch sử gửi).
- **Thông báo trong app**: `Notification` + `NotifiInUser`/`NotificationOfEachUser`.

---

## 11. Tiện ích phụ trợ

### 11.1 Tasking + Bàn giao
- `Tasking` — công việc, `CommentTasking`, `UserInTask`.
- `SectionWork` — phân khu công việc, `HandoverNote` — ghi chú bàn giao.

### 11.2 Nhật ký + Phản hồi
- `ActivityLogs`/`ActivityLogsNewVersion` (gộp!), `Logger`, `LichSuTheoDoi`.
- `CommentHistory`, `CommentTour` — bình luận theo context.
- `FeedbackTour` — phản hồi tour, `KeyPerformanceIndicator` — KPI.
- `ReasonSwitch`/`DetailReasonSwitch` — lý do đổi/hủy (đơn, vé, slot).

### 11.3 Nội dung CMS
- `Posts`, `CategoriesPost`, `CommentsPost`, `Like` — blog/tin.

### 11.4 Cấu hình hệ thống
- `ConfigCompany` — cấu hình công ty (tên, logo, địa chỉ, tax).
- `Config` — cấu hình chung.
- `ConfigContract`, `ConfigSurcharge`, `ConfigCommission` — cấu hình theo mảng.
- `ExchangeRate`/`Rate` — tỷ giá (đa ngoại tệ).
- `SettingPage`, `UserGridSettings` — tùy chỉnh UI per-user.
- `BackgroundJob` — tác vụ nền.
- `FileUpload` — quản lý file (R2).

---

## 12. Dòng dịch vụ khác & Tích hợp (hoàn tất khảo sát tính năng)

Ngoài tour trọn gói, hệ cũ vận hành như **nền tảng đa dịch vụ lữ hành** — nhiều "sản phẩm" dùng chung hạ tầng `Order`/`Customer`/`Voucher` nhưng có entity + luồng riêng. (Xác định từ inventory ~50 module + khảo sát module thật; mục nào suy từ tên ghi rõ.)

### 12.1 Dòng sản phẩm độc lập (mỗi cái ứng một `BookingType`)
- **Vé máy bay (AirPlaneTicket / VMB)** — vé lẻ + vé đoàn. Luồng thật: tạo → duyệt (`VMB_DUYET`) → chốt đơn (status 108/109) → xuất báo giá/hóa đơn; gắn duyệt hóa đơn + hoa hồng. Provider = Airline.
- **Đặt phòng khách sạn (BookingHotel)** — đặt phòng lẻ, tách khỏi tour trọn gói; có `HotelState` riêng. Provider = Hotel.
- **Visa (Visa)** — dịch vụ làm visa (bán lẻ hoặc gắn tour); `VisaTourGuide` cho HDV cross-border.
- **Xe (CarManagement, `BookingType=9`)** — điều xe, `CarType`/`Vehicle`. Provider = Vehicle.

> **Ngụ ý schema:** `Order` là trung tâm **đa dịch vụ**. Thêm `ServiceLine` (Tour|AirTicket|Hotel|Visa|Car) trên `Order` + bảng chi tiết riêng theo dòng (polymorphic có kiểm soát) — KHÔNG nhồi mọi field vào 1 bảng. Dòng ít field (Visa) → bảng con 1-1; dòng phức tạp (Tour) → cụm bảng riêng (§B3). Thiết kế "đa dịch vụ ngay từ đầu" để không refactor `Order` về sau.

### 12.2 Dự trù & Báo giá
- **DuTruTours (dự trù chi phí tour)** *(suy từ tên)* — ước tính chi phí NCC trước khi chốt (ngân sách chuyến) → bảng `TourCostEstimate` tách khỏi `OrderCost` (chi thực tế).
- **BaoGia (báo giá)** — sinh báo giá VN/EN từ mẫu tour + `PriceScenario`; là **output**, không cần bảng riêng.

### 12.3 Tích hợp ngoài
- **BankHub** — nhận webhook/đối soát giao dịch ngân hàng, tự gạch nợ phiếu thu → `BankTransaction` + matching với `ReceiptVoucher`.
- **BillMiFi / InvoiceBranch** *(suy từ tên)* — phát hành **hóa đơn điện tử** qua nhà cung cấp, theo chi nhánh → `EInvoice` + trạng thái phát hành.
- **CallCenter** *(suy từ tên)* — tích hợp tổng đài, log cuộc gọi gắn khách → `CallLog(CustomerId, Direction, Duration, RecordingUrl)`.
- **Pancake / ZaloUID / ZaloZns / SMS / Email** — kênh marketing & CSKH (đã ở §10).

### 12.4 Vận hành nội bộ & báo cáo
- **Workflow** — engine cấu hình luồng duyệt = `ApprovalProcess` (§7.3), dùng chung mọi phiếu.
- **WorkSpaceView / HomePage** — dashboard theo vai trò.
- **SystemReport / MoneyReport / ReportCommission / KeyPerformanceIndicator** — cụm báo cáo (đọc thuần → Materialized View, xem tối ưu DB §F4).
- **FeedBackTour** — phản hồi sau tour (đánh giá/NPS).
- **BackgroundJobs / HandleUploadFile / Upload / LogSystem** — hạ tầng (job nền, upload R2, audit).

**Phạm vi:** MVP tập trung **Tour trọn gói** (P1-P6). Air/Hotel/Visa/Car + HĐĐT/BankHub/CallCenter là **mở rộng sau MVP**, nhưng `Order` phải mang sẵn `ServiceLine` từ đầu.

---

## Tóm tắt: Khối ưu tiên SaaS (MVP bán được)

| Ưu tiên | Khối | Lý do |
|---|---|---|
| P0 | Tenancy + Auth/RBAC | Đã làm 0a, cần 0b |
| P1 | Catalog (TourSample+Tour+Itinerary+Pricing) | Sản phẩm cốt lõi, không có = không bán được |
| P2 | Customer + Lead | Phễu bán |
| P3 | Booking (Order+TourCustomer+BookingTicket) | Giao dịch |
| P4 | Provider + OrderChi | Điều hành + chi phí |
| P5 | Finance (Receipt/Payment + Approval) | Tiền |
| P6 | Commission/ProfitSharing | Chốt lãi |
| P7 | Marketing | Scale |

Lưu ý cho redesign: **bỏ field thừa** (HR trên User), **gộp bảng trùng** (ActivityLogs, Comission), **normalize danh sách CSV** (`IdsNguoiTheoDoi`, `ManagerIds`), **sửa tên nhầm** (Receipt/Payment), **thay phân quyền tự chế bằng RBAC**.
