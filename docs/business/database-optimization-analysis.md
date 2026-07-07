# TourKit — Phân tích & Tối ưu Database cho SaaS

> Đối chiếu schema hệ gốc (124 bảng, MySQL/SQL Server) với yêu cầu SaaS multi-tenant.
> Cập nhật: 2026-07-07. Đọc kèm `business-spec.md`.

---

## PHẦN A — Chẩn đoán vấn đề schema hệ cũ

### A1. Multi-tenant "DB-per-tenant" → tốn kém, khó scale
**Hệ cũ:** `TenantResolver` map domain → connection string; **mỗi tenant 1 DB riêng**.
**Vấn đề:**
- N tenant = N database → backup, migration, monitoring x N. Chi phí vận hành tăng tuyến tính.
- Không chạy được query liên tenant (báo cáo platform, billing).
- Caching phức tạp (`CacheStore` per-tenant + file signal multi-worker).

**Giải pháp (đã chốt Phase 0a):** **Shared DB + `TenantId`** trên mỗi bảng nghiệp vụ + EF Core global query filter. Mọi entity implement `ITenantEntity`. Đã làm, giữ nguyên.

---

### A2. Bảng trùng lặp / dư thừa
| Bảng dư | Vấn đề | Hành động |
|---|---|---|
| `ActivityLogs` vs `ActivityLogsNewVersion` | Cùng mục đích, 2 phiên bản | Gộp thành `AuditLog` |
| `Comission` vs `CommissionCampaign` vs `CommissionProfitLevel` vs `ConfigCommission` | 4 bảng hoa hồng chồng chéo | Thiết kế 2 bảng: `CommissionRule` + `CommissionCampaign` |
| `Notification` vs `NotifiInUser` vs `NotificationOfEachUser` | 3 bảng thông báo | Gộp: `Notification` + `NotificationRecipient` |
| `CustomerCare` vs `CustomerCareOfEachUser` | Chăm sóc + phân công tách rời | Gộp vào `CustomerInteraction` |
| `PhongBanSubjectQuyen` + `SubjectQuyenFunctionQuyen` + `UserPermission` | 5 bảng phân quyền tự chế | Thay bằng RBAC: 3 bảng |
| `ReceiptVoucher` vs `ReceiptVoucherApproval` vs `ReceiptVoucherApprovalStepUser` | Duyệt tách 2 bảng | Gộp vào engine approval chung |

---

### A3. Anti-pattern "CSV trong cột"
Hệ cũ lưu **danh sách ID dưới dạng chuỗi CSV** trong 1 cột:
- `Customer.IdsNguoiTheoDoi`, `Tour.IdsNguoiTheoDoi`, `Tour.ManagerIds`
- `BookingTicket.NguoiPhuTrachs`, `BookingTicket.IdsFollower`
- `ReceiptVoucher.IdsNguoiTheoDoi`, `PaymentVoucher.IdsFollower`
- `Customer.tagCustomer`, `Tour.marketId` (dù có MarketType)

**Vấn đề:** không join được, không index được, mâu thuẫn dữ liệu, chậm khi list dài.

**Giải pháp:** **normalize** bằng bảng nối nhiều-nhiều:
- `CustomerWatcher(CustomerId, UserId)`, `TourManager(TourId, UserId)`, `CustomerTag(CustomerId, TagId)`.

---

### A4. Audit trail không nhất quán
**Hệ cũ:** mỗi entity tự có `INS_DTTM`, `INS_UID`, `UPD_DTTM`, `UPD_UID`, `AUD_VER` — đặt tên lộn xộn (snake_case + ALL_CAPS + camelCase lẫn lộn).

**Giải pháp (đã chốt):** `BaseEntity` thống nhất: `Id (Guid)`, `CreatedAt`, `UpdatedAt`, `IsDeleted`. Bỏ `AUD_VER` (dùng EF Core concurrency token `RowVersion` nếu cần optimistic locking). Ai sửa (`CreatedBy`, `UpdatedBy`) — thêm trường `ITenantEntity` mở rộng, hoặc bảng audit riêng.

---

### A5. Đặt tên hỗn tạp Việt–Anh, nhiều ý nghĩa
| Hệ cũ | Vấn đề | Đổi thành |
|---|---|---|
| `PhongBan` | Tiếng Việt | `Department` |
| `LoaiPhieuTaiChinh` | Dài | `VoucherCategory` |
| `TrangThaiDonHang` | Dài | `OrderStatus` (lookup) |
| `LoaiDonHang` | Dài | `OrderType` (lookup) |
| `ReceiptVoucher` (thực ra là phiếu chi NCC) | **NHẦM** | `PaymentVoucher` (chi) / `ReceiptVoucher` (thu) — đặt đúng chiều tiền |
| `Comission` | Sai chính tả | `Commission` |
| `NguoiPhuTrach`, `IdsNguoiTheoDoi` | Tiếng Việt, CSV | Bảng nối `Assignee`/`Watcher` |
| `tien_chua_phu_thu`, `so_luong`, `phan_thu` | snake_case VN | Đặt tên nghiệp vụ rõ (xem §B) |

**Convention SaaS (đã chốt):** PascalCase tiếng Anh, EF Core tự map snake/Pascal sang DB.

---

### A6. Trộn doanh thu + chi phí vào Order
`Order` chứa cả `Total_Thu_Money`, `Total_Chi_Money`, `ApprovedRevenue`, `UnapprovedSpent`… → vừa là tổng, vừa là chi tiết, khó báo cáo, khó audit.

**Giải pháp:** tách ra:
- `Order` — chỉ metadata (khách, tour, trạng thái, người phụ trách).
- `OrderRevenue` — dòng doanh thu (theo loại, duyệt/chưa).
- `OrderCost` — dòng chi phí (theo NCC, theo dịch vụ) — thay `OrderChi` + `OrderProviderMoney`.

---

### A7. Giá 4 nhóm tuổi lặp 4 lần
`TourSample` và `TourCustomer` đều lặp `price_per_slot/childs/childs_small/baby` + `commission_*` + `discount_*` + `surcharge_*` (mỗi nhóm 4-5 cột × 4 nhóm = ~20 cột).

**Vấn đề:** nếu thêm nhóm tuổi mới (vd "người cao tuổi") → sửa schema. Cứng.

**Giải pháp:** bảng `PriceTier` (`AgeGroup` enum: Adult/Child/ChildSmall/Baby, `UnitPrice`, `Commission`, `Discount`, `Surcharge`). Mỗi TourSample/TourCustomer có nhiều dòng `PriceTier`. Dễ mở rộng, dễ báo cáo.

---

### A8. User chứa field HR thừa
`User` có ~40 trường: `NgaySinh`, `Noisinh`, `HonNhan`, `DanToc`, `TonGiao`, `TrinhDo`, `NgayVao`, `SoTaiKhoan`, `NganHang`… → phần lớn là HR profile, không cần cho SaaS MVP lữ hành.

**Giải pháp:** `User` giữ tối giản (`Id, FullName, Email, Phone, Status, DepartmentId, PositionId, Avatar`). Trường HR (nếu muốn) tách bảng `UserProfile` (1-1) — và chỉ thêm khi có yêu cầu.

---

## PHẦN B — Schema SaaS đề xuất (theo khối nghiệp vụ)

> Quy ước: mọi bảng nghiệp vụ (không phải lookup platform) implement `ITenantEntity` → có `TenantId Guid`. Kế thừa `BaseEntity` (`Id Guid`, `CreatedAt`, `UpdatedAt`, `IsDeleted`). Index bắt đầu bằng `TenantId`.

### B1. Nền tảng + Identity
```
Tenant(Id, Name, Slug[unique], Status, PlanId, CreatedAt...)
Subscription(TenantId, PlanId, Status, StartedAt, ExpiresAt, Seats)
Plan(Id, Name, MaxUsers, MaxTours, Features[json], Price)
User(TenantId, FullName, Email, Phone, PasswordHash, Status, DepartmentId, PositionId, Avatar, LastLoginAt)
Department(TenantId, Name, ParentId)        — phòng ban cây
Position(TenantId, Name)                     — vị trí
Role(TenantId, Name)                         — vai trò RBAC
Permission(Id, Code, Group)                  — permission platform (không per-tenant)
RolePermission(RoleId, PermissionId)
UserRole(UserId, RoleId)
RefreshToken(UserId, Token, ExpiresAt, RevokedAt)
```

### B2. CRM Customer
```
Lead(TenantId, Name, Phone, Email, SourceId, Status, AssignedToUserId,
     SalesAssignMode, RotationOrder)         — khách tiềm năng + round-robin
Customer(TenantId, Code[uniq/tenant], FirstName, LastName, Gender, Birthday,
         Phone, Email, Address, TaxCode, CompanyName,
         SourceId, TypeId, MarketId, LeadId?,
         Status, BalanceTemp, Note)
CustomerSource(TenantId, Name)               — lookup
CustomerType(TenantId, Name)                 — lookup
CustomerWatcher(CustomerId, UserId)          — normalize IdsNguoiTheoDoi
CustomerInteraction(TenantId, CustomerId, Type, Title, Detail, Status,
                    ScheduledAt, Feedback, CreatedBy)  — gộp CustomerCare
Tag(TenantId, Name, Color)
CustomerTag(CustomerId, TagId)
```

### B3. Catalog Tour (P1 — cốt lõi)
```
TourSample(TenantId, Code, Title, TourType, TypeOf,
           DepartureDate, EndDate, BookingDate, ReservationHours,
           TotalSlots, OversoldSeats,
           PickupPlace, DropoffPlace, TransportMode,
           TermsNote, TermsNoteEn,
           Status, CreatedBy)
TourItinerary(TenantId, TourSampleId?, TourId?, DayIndex, Title, Detail)
PriceTier(TenantId, TourSampleId, AgeGroup[Adult|Child|ChildSmall|Baby],
          UnitPrice, CommissionType[Pct|Amount], CommissionValue,
          Surcharge, Discount)               — thay 20 cột giá lặp
PriceScenario(TenantId, TourSampleId, FromQty, ToQty, UnitPrice)
MarketType(TenantId, Name)                   — lookup (thị trường)
```

### B4. Booking + Chuyến
```
BookingTicket(TenantId, Code, CustomerName, Phone, Email,
              AdultQty, ChildQty, BabyQty, AdultPrice, ChildPrice, BabyPrice,
              Source, Status, IsConfirmed, ConfirmedAt, ConfirmedBy,
              TourSampleId?, TourId?, CustomerId?)
Order(TenantId, Code, TourId, CustomerId, TourSampleId, Status,
      AssignedToUserId, TotalRevenue, TotalCost, TotalRefund,
      ApprovedRevenue, ApprovedCost, IsPaymentRecognized)
TourCustomer(TenantId, TourId, CustomerId, OrderId,
             AgeGroup, Qty, UnitPrice, Surcharge, Discount, Commission,
             UpfrontAmount, SeatCode, IsMainContact, Status, Signature)
CancelSeat(TenantId, TourCustomerId, Reason, CancelledAt, CancelledBy)
DetailReasonSwitch(TenantId, Context[Order|Ticket|Seat], RefId, ReasonId, Note)
ReasonSwitch(TenantId, Name)                 — lookup lý do
TourManager(TourId, UserId, Role[Manager|Watcher|Assignee])  — normalize
```

### B5. Tour cụ thể (điều hành)
```
Tour(TenantId, Code, Title, TourSampleId, ParentTourId,
     DepartureDate, EndDate, DepartureTime, EndTime,
     AdultCount, ChildCount, TypeOfService, MarketId, ExchangeRateId,
     AssignedToUserId, Status, IsClosed, ClosedAt,
     CommissionClosedAt)
TourGuide(TenantId, Name, Phone, Email, Skill, Languages, Avatar, IsExternal)
TourGuideAssignment(TourId, TourGuideId, Role, Revenue, Expense)
```

### B6. Provider + Chi phí
```
Provider(TenantId, Code, Name, Type[Hotel|Vehicle|Restaurant|Guide|Airline|Other],
         Phone, Email, ContactPerson, TaxCode, BankAccount, BankName,
         ClassHotelId?, Rating, Status)
ProviderService(TenantId, ProviderId, ServiceId, Name, PricingMode)
ProviderServicePricing(TenantId, ProviderServiceId, FromDate, ToDate, UnitPrice)
ProviderServiceOrder(TenantId, ProviderServiceId, OrderId, Qty, UnitPrice, Total)
Service(TenantId, Name, Type)                — catalog dịch vụ
OrderCost(TenantId, OrderId, ProviderId, ServiceId, DayIndex,
          ExpectedAmount, ActualAmount, Deposit, Surcharge, Vat, Status,
          SignedAt, SignedBy)                — thay OrderChi + OrderProviderMoney
Vehicle(TenantId, Plate, CarTypeId, ProviderId?)
CarType(TenantId, Name, SeatCount)
ClassHotel(TenantId, Name, Stars)            — lookup
AccountBalance(TenantId, UserId, PaymentMethodId, Balance)
```

### B7. Tài chính
```
VoucherCategory(TenantId, Name, Direction[In|Out])   — thay LoaiPhieuTaiChinh
PaymentMethod(TenantId, Name, Type[Cash|Transfer|Card|Fund])
ExchangeRate(TenantId, Currency, Rate, EffectiveAt)

ReceiptVoucher(TenantId, Code, Title, IssuedAt, OrderId, CustomerId,
               Amount, PaymentMethodId, CategoryId, Partner, BankAccount,
               Status, IsClosed, ParentId, ApprovedBy)     — TIỀN VÀO
PaymentVoucher(TenantId, Code, Title, IssuedAt, OrderId, OrderCostId, ProviderId,
               Amount, PaymentMethodId, CategoryId, Partner, BankAccount,
               Status, IsClosed, ParentId, ApprovedBy)     — TIỀN RA
VoucherSplit(VoucherId, Amount, Note)             — tách phiếu (ParentId)

ApprovalProcess(TenantId, Name, Method[Sequential|Parallel|Any], Status)
ApprovalStep(TenantId, ProcessId, Order, PositionId)
ApprovalStepUser(StepId, UserId)
ApprovalHistory(TenantId, ProcessId, RefType, RefId, StepOrder, UserId,
                Decision[Approve|Reject], Comment, DecidedAt)
```

### B8. Hoa hồng
```
CommissionRule(TenantId, CustomerTypeId, UserId?, PctOrAmount, Value)  — thay Comission
CommissionCampaign(TenantId, Name, FromDate, ToDate, RuleJson, Status)
CommissionProfitLevel(TenantId, FromProfit, ToProfit, CommissionPct)
ProfitSharing(TenantId, TourId, UserId, Pct, CommissionAmount, RevenueBase)
```

### B9. Marketing (tách module, làm sau)
```
MarketingCampaign(TenantId, Name, Channel[Email|Sms|Zalo], TemplateId, Status)
MarketingTemplate(TenantId, Channel, Name, Subject, Body)
MarketingSendHistory(TenantId, CampaignId, ToUserId?, ToCustomer?, Status, SentAt)
Notification(TenantId, Title, Body, Type, CreatedAt)
NotificationRecipient(NotificationId, UserId, IsRead, ReadAt)
```

### B10. Phụ trợ
```
AuditLog(TenantId, Actor, Action, EntityType, EntityId, Before[json], After[json], At)
FileUpload(TenantId, Name, Key, Size, Mime, Url, UploadedBy)
BackgroundJob(TenantId, Type, Payload[json], Status, RunAt, Error)
Task(TenantId, Title, SectionId, Status, AssigneeId, DueAt)
TaskAssignee(TaskId, UserId)
TaskComment(TaskId, UserId, Body, At)
ConfigCompany(TenantId, Name, LogoUrl, Address, TaxCode, Phone, Email)
```

---

## PHẦN C — Nguyên tắc thiết kế áp dụng

1. **Mọi bảng nghiệp vụ có `TenantId`** + index mở đầu bằng `TenantId`. Query filter tự cô lập.
2. **Kế thừa `BaseEntity`**: `Id (Guid)`, `CreatedAt`, `UpdatedAt`, `IsDeleted`. Bỏ `AUD_VER`, `INS_UID/UPD_UID` lộn xộn → thêm `CreatedBy`/`UpdatedBy` chuẩn.
3. **Lookup** (loại khách, nguồn, thị trường, loại phiếu…): bảng riêng `TenantId + Name`, không hardcode enum trong code trừ khi cố định platform.
4. **Normalize mọi danh sách**: không lưu CSV trong cột. Dùng bảng nối.
5. **Giá theo nhóm tuổi** dùng bảng `PriceTier` (mở rộng được), không lặp 4×.
6. **Tiền tệ**: lưu `decimal(18,2)`, áp `ExchangeRate` khi báo cáo — không lưu multi-currency trong cùng dòng.
7. **Workflow duyệt dùng chung** (`ApprovalProcess` + `ApprovalHistory`), không copy cho mỗi loại phiếu.
8. **RBAC chuẩn** (Role/Permission/RolePermission/UserRole), bỏ SubjectQuyen/FunctionQuyen.
9. **Soft-delete thống nhất** qua `IsDeleted` + query filter (đã làm Phase 0a).
10. **Đặt tên PascalCase tiếng Anh**, EF Core map sang DB. Bỏ trộn Việt-Anh.

---

## PHẦN D — Bảng so sánh "trước/sau" (số lượng entity)

| Khối | Hệ cũ (bảng) | SaaS (bảng) | Ghi chú |
|---|---|---|---|
| Foundation/Identity | ~15 | ~12 | RBAC thay SubjectQuyen, +Plan/Subscription |
| CRM | ~8 | ~9 | +Lead tách, normalize watcher/tag |
| Catalog | ~5 | ~6 | +PriceTier, PriceScenario rõ |
| Booking/Tour | ~10 | ~10 | BookingTicket/Order/TourCustomer rõ |
| Provider | ~10 | ~9 | gộp OrderChi→OrderCost |
| Finance | ~8 | ~9 | Receipt/Payment đúng tên + Approval chung |
| Commission | ~4 | ~4 | gộp Comission+config |
| Marketing | ~12 | ~5 | gộp 3 kênh + history |
| Phụ trợ | ~12 | ~8 | gộp ActivityLog, +Task riêng |
| **Tổng** | **~124** | **~72** | **giảm ~42%**, rõ ràng hơn |

---

## PHẦN E — Lộ trình triển khai (database-first theo phase)

| Phase | Khối DB | Mục tiêu |
|---|---|---|
| 0a ✅ | Tenant + BaseEntity | Kernel multi-tenant (xong) |
| 0b | Identity/Subscription/Plan | Auth + RBAC + onboarding tenant |
| 1 | Catalog (TourSample/Tour/Itinerary/PriceTier/Scenario) | Sản phẩm cốt lõi |
| 2 | CRM (Customer/Lead/Interaction) | Phễu bán |
| 3 | Booking (BookingTicket/Order/TourCustomer/CancelSeat) | Giao dịch |
| 4 | Provider + OrderCost | Điều hành + chi phí |
| 5 | Finance (Receipt/Payment + Approval + ExchangeRate) | Tiền |
| 6 | Commission/ProfitSharing | Chốt lãi |
| 7 | Marketing + Task + AuditLog | Scale + phụ trợ |

Mỗi phase: entity + config + migration + endpoint + test cô lập tenant. Chuyển SQL Server production khi cần (đổi `Database:Provider`).
