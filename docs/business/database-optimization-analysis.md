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

### A6. ~~Trộn doanh thu + chi phí vào Order~~ → GIỮ LẠI (denormalize có chủ đích)

> ⚠️ **ĐÍNH CHÍNH (sau khi đọc read pattern thật — xem §F):** Đề xuất ban đầu của tôi là tách
> `OrderRevenue`/`OrderCost` thành bảng riêng. **SAI.** Màn "Tất cả tour" (`TourSharedSearch_v3`)
> sort theo `Total_Thu_Money`, `Thuc_Thu`, `Total_Chi_Money`, `ApprovedRevenue` trực tiếp trên grid
> phân trang. Màn "Công nợ khách" cũng sort theo `SUM(Total_Thu_Money)`. Nếu tách ra bảng con,
> mỗi lần load grid phải `GROUP BY` + `JOIN` aggregate → chậm, không index được trên cột sort.

**Giải pháp sửa lại (câu chốt chung với `business-spec §5.1`):** GIỮ cột tổng doanh thu/chi phí
(`TotalRevenue`, `TotalCost`, `ApprovedRevenue`…) TRỰC TIẾP trên `Order` — **denormalize có chủ đích**;
chỉ tách **chi tiết dòng chi phí NCC** ra `OrderCost` (đổi tên từ `OrderChi`).
Cơ chế giữ cột tổng luôn đúng (đồng bộ ở domain service trong transaction — KHÔNG trigger, optimistic locking, job đối soát) → xem **§I**.

---

### A7. ~~Giá 4 nhóm tuổi lặp 4 lần~~ → GIỮ LẠI (denormalize có chủ đích)

> ⚠️ **ĐÍNH CHÍNH:** Đề xuất ban đầu của tôi là tách giá ra bảng `PriceTier`. **SAI.** Màn
> "Tất cả tour" sort/filter theo `price_per_slot` (giá người lớn) trực tiếp trên grid. Nếu đẩy
> vào bảng con `PriceTier`, sort phải JOIN + tìm dòng `AgeGroup=Adult` → không SARGable, không
> index được, grid không sort được. Đây chính xác nỗi lo bạn chỉ ra.

**Giải pháp sửa lại:** GIỮ 4 nhóm tuổi dưới dạng cột trực tiếp trên `TourSample` và `TourCustomer`
(như hệ cũ). Đổi tên PascalCase rõ ràng (`PriceAdult/PriceChild/PriceChildSmall/PriceBaby`).
Chấp nhận lặp ~20 cột vì: nhóm tuổi cố định nghiệp vụ (người lớn/trẻ em/trẻ nhỏ/em bé) —
rất hiếm khi thêm. Nếu sau này cần mở rộng → dùng cột `ExtraPricing jsonb` (PostgreSQL).

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

### B3. Tour (cốt lõi) — mẫu TPT: 1 bảng gốc + 2 bảng phụ theo loại

> **Quyết định thiết kế:** Gộp `tour_samples` + `tours` của hệ cũ thành 1 bảng gốc `Tour`
> + 2 bảng phụ 1-1 theo `Kind` (Template | Departure). Pattern **TPT (Table-per-Type)**,
> EF Core 9 hỗ trợ native qua `ToTable()` + kế thừa. Lý do: gom cột chung (Code, Title,
> ngày, trạng thái...) vào 1 chỗ, cột riêng (giá vs điều hành) tách bảng phụ.
> **TourCustomer KHÔNG gộp** — quan hệ 1 Tour : N TourCustomer (vi phạm 1NF nếu gộp).

**Bảng gốc `Tour`** (cột chung cho cả mẫu và chuyến — sort/filter/index 1 chỗ):
```
Tour(
  Id, TenantId,                              — kernel (BaseEntity + ITenantEntity)
  Kind ENUM(Template, Departure),            — phân biệt: mẫu | chuyến cụ thể
  Code, Title, TourType, TypeOf,
  DepartureDate, EndDate, BookingDate,
  TotalSlots, OversoldSeats,
  Services,                                  — dịch vụ inklusiv (JSONB nếu cần)
  PickupPlace, DropoffPlace, TransportMode,
  MarketId, ParentTourId NULL,               — Departure trỏ về Template nguồn
  DepartureTime, EndTime,
  Status, CreatedBy, CreatedAt, UpdatedAt, IsDeleted
)
INDEX: (TenantId, Kind, Status), (TenantId, DepartureDate), (TenantId, Code), (TenantId, ParentTourId)
```

**Bảng phụ 1: `TourTemplateFields`** (chỉ Kind=Template — báo giá/catalog):
```
TourTemplateFields(
  TourId PK/FK→Tour,
  ReservationHours,                          — thời hạn giữ chỗ
  — Giá 4 nhóm tuổi GIỮ TRỰC TIẾP (sort/filter theo PriceAdult trên grid mẫu):
  PriceAdult, CommissionAdultType, CommissionAdultValue,
  PriceChild, CommissionChildType, CommissionChildValue,
  PriceChildSmall, CommissionChildSmallType, CommissionChildSmallValue,
  PriceBaby, CommissionBabyType, CommissionBabyValue,
  TermsNote, TermsNoteEn, TourPrice, Discount
)
```

**Bảng phụ 2: `TourDepartureFields`** (chỉ Kind=Departure — điều hành/chốt chuyến):
```
TourDepartureFields(
  TourId PK/FK→Tour,
  AmountAdults, AmountChildren, SoLuong,
  AssignedToUserId, IsClosed, ClosedAt,
  CommissionStatus, CommissionClosedAt,
  IsPaymentRecognized, PhanThu, MaYeuCau,
  QuoteSample, CheckList,
  ExchangeRateId
)
```

**Bảng phụ trợ Tour:**
```
TourItinerary(TourId, DayIndex, Title, Detail, TourKind)  — lịch trình ngày (áp cả mẫu + chuyến)
PriceScenario(TourTemplateId, FromQty, ToQty, UnitPrice)  — kịch bản giá theo đoàn
MarketType(TenantId, Name, ParentId)         — lookup cây thị trường (đệ quy trong proc cũ)
TourAssignee(TourId, UserId, Role[Manager|Watcher|Assignee])  — normalize IdsNguoiTheoDoi/ManagerIds
TourGuide(TenantId, Name, Phone, Email, Skill, Languages, Avatar, IsExternal)
TourGuideAssignment(TourDepartureId, TourGuideId, Role, Revenue, Expense)
```

**EF Core 9 mapping (TPT native):**
```csharp
public abstract class Tour : BaseEntity, ITenantEntity {
    public TourKind Kind { get; set; }       // Template | Departure
    public string Code { get; set; }
    public string Title { get; set; }
    public DateTime? DepartureDate { get; set; }
    public int? Status { get; set; }
    // ... cột chung
}
public sealed class TourTemplate : Tour {
    public decimal PriceAdult { get; set; }
    // ... cột riêng mẫu
}
public sealed class TourDeparture : Tour {
    public int AmountAdults { get; set; }
    // ... cột riêng chuyến
}
// Config:
modelBuilder.Entity<Tour>().ToTable("Tours");
modelBuilder.Entity<TourTemplate>().ToTable("TourTemplateFields", t => t.StartsWithBaseTable());
modelBuilder.Entity<TourDeparture>().ToTable("TourDepartureFields", t => t.StartsWithBaseTable());
// Query:
db.Tours.OfType<TourTemplate>()...           // grid mẫu
db.Tours.OfType<TourDeparture>()...          // grid chuyến
db.Tours.Where(t => t.Title.Contains(kw))... // autocomplete 1 bảng, không UNION
```

### B4. Booking (Order + TourCustomer + BookingTicket)

> ⚠️ **ĐÍNH CHÍNH (sau khi đọc read pattern thật — xem §F):**
> - KHÔNG tách `OrderRevenue`/`OrderCost` thành bảng riêng — grid sort theo cột tổng
>   (`TotalRevenue`, `ApprovedRevenue`) trực tiếp, phải giữ denormalize trên `Order`.
> - KHÔNG tách giá 4 nhóm tuổi ra `PriceTier` — grid mẫu sort theo `PriceAdult`.
> - `TourCustomer` KHÔNG gộp vào Tour (quan hệ 1:N, vi phạm 1NF).

```
BookingTicket(TenantId, Code, CustomerName, Phone, Email,
              AdultQty, ChildQty, BabyQty, AdultPrice, ChildPrice, BabyPrice,
              Source, Status, IsConfirmed, ConfirmedAt, ConfirmedBy,
              TourTemplateId?, TourDepartureId?, CustomerId?)
Order(TenantId, Code, TourDepartureId, CustomerId, TourTemplateId?, Status,
      AssignedToUserId,
      — Tổng tài chính GIỮ TRỰC TIẾP (denormalize, sort/filter trên grid):
      TotalRevenue, TotalRevenueRoot, TotalCost, TotalCostSale,
      ApprovedRevenue, ApprovedRevenueSingle, UnapprovedRevenue,
      ApprovedCost, UnapprovedCost, TotalRefund,
      IsPaymentRecognized)
TourCustomer(TenantId, TourDepartureId, CustomerId, OrderId,
             — Giá/phụ thu/chiết khấu/hoa hồng 4 nhóm tuổi GIỮ TRỰC TIẾP (như hệ cũ):
             Qty, AmountAdults, AmountChildren, AmountChildrenSmall, AmountBaby,
             PriceAdult, PriceChild, PriceChildSmall, PriceBaby,
             Surcharge, ChildSurcharge, ChildSurchargeSmall, BabySurcharge,
             Discount, ChildDiscount, ChildDiscountSmall, BabyDiscount,
             Commission, ChildCommission, ChildCommissionSmall, BabyCommission,
             UpfrontAmount, SeatCode, IsMainContact, Status, Signature)
CancelSeat(TenantId, TourCustomerId, Reason, CancelledAt, CancelledBy)
DetailReasonSwitch(TenantId, Context[Order|Ticket|Seat], RefId, ReasonId, Note)
ReasonSwitch(TenantId, Name)                 — lookup lý do
```

> Lưu ý: B4 gộp vào B3 (Tour TPT), không còn B5 riêng. Provider/Finance ở B6, B7 như cũ.

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
          SignedAt, SignedBy)                — dòng chi phí NCC (giữ, chỉ đổi tên OrderChi→OrderCost)
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
5. **Giá theo nhóm tuổi GIỮ TRỰC TIẾP 4 cột** (`PriceAdult/PriceChild/PriceChildSmall/PriceBaby`) trên `TourTemplateFields`/`TourCustomer` — KHÔNG tách `PriceTier`. Lý do: grid mẫu sort/filter theo `PriceAdult`; tách bảng con làm cột sort không SARGable (xem §A7, §F2). Nhóm tuổi cố định nghiệp vụ nên chấp nhận lặp cột; mở rộng hiếm gặp → dùng `ExtraPricing jsonb`.
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
| Catalog | ~5 | ~6 | Tour TPT + PriceScenario; giá 4 nhóm tuổi giữ cột (không PriceTier) |
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
| 1 | Catalog (Tour TPT + Itinerary + PriceScenario) | Sản phẩm cốt lõi |
| 2 | CRM (Customer/Lead/Interaction) | Phễu bán |
| 3 | Booking (BookingTicket/Order/TourCustomer/CancelSeat) | Giao dịch |
| 4 | Provider + OrderCost | Điều hành + chi phí |
| 5 | Finance (Receipt/Payment + Approval + ExchangeRate) | Tiền |
| 6 | Commission/ProfitSharing | Chốt lãi |
| 7 | Marketing + Task + AuditLog | Scale + phụ trợ |

Mỗi phase: entity + config + migration + endpoint + test cô lập tenant.
Provider DB: SQLite ở dev → **PostgreSQL ở production** (đổi `Database:Provider`).

---

## PHẦN F — Read pattern thật + giải pháp (PHẢI ĐỌC TRƯỚC KHI THIẾT KẾ BẢNG)

> Phần này ghi lại **cách màn hình thật load dữ liệu** trong hệ gốc (đọc từ stored proc
> thật). Đây là cơ sở cho mọi quyết định tách/gộp bảng ở §B. Bài học cốt lõi: **schema
> phải phục vụ read pattern, không chỉ đẹp trên lý thuyết.**

### F1. Các màn grid nóng và read pattern của chúng

Đọc từ 4 stored proc chính của hệ gốc:

| Màn | Stored proc | Join những gì | Sort theo field nào |
|---|---|---|---|
| **Tất cả chuyến** | `TourSharedSearch_v3` | `tours` ← `tour_customers`(main) ← `customers` ← `Orders` ← `OrderChi` agg ← `Rate` agg ← `users`(seller, phụ trách) ← `CarType` ← `customer_source` = **10+ bảng** | `Total_Thu_Money`, `Thuc_Thu`, `Total_Chi_Money`, `status`, `departure_date`, đánh giá trung bình |
| **Tất cả mẫu tour** | `TourSampleSearch_v3` | `tour_samples` ← `Orders` ← agg thu/chi ← count khách (đã bán/giữ chỗ) | `price_per_slot`, `status`, `departure_date`, tổng doanh thu |
| **Khách hàng** | `uspCustomerSearch` | `customers` ← `customer_type` ← agg `Orders`(`count`, `SUM money`) | **`total_money desc`** (field tính ra từ SUM!) |
| **Phiếu thu/chi** | `uspFilterPaymentVoucher_v1` | UNION `tours`+`tour_samples` → `#AllTour` ← `Orders` ← `PaymentMethod` | ngày, trạng thái, số tiền |
| **NCC** | `uspGetListProvider` | `providers` ← `provider_services` ← `services` ← `Order_Chi` | tên, dịch vụ |

### F2. Bài học rút ra (chỉnh sửa đề xuất ban đầu)

| Đề xuất ban đầu của tôi | Verdict sau khi đọc read pattern | Lý do |
|---|---|---|
| Tách `OrderRevenue`/`OrderCost` thành bảng riêng | ❌ **BỎ** — GIỮ cột tổng trên `Order` | Grid sort theo `TotalRevenue`, `ApprovedRevenue` trực tiếp. Tách = GROUP BY + JOIN mỗi lần load → chậm |
| Tách giá 4 nhóm tuổi ra `PriceTier` | ❌ **BỎ** — GIỮ 4 nhóm tuổi trực tiếp | Grid mẫu sort theo `PriceAdult`. Tách = JOIN tìm dòng AgeGroup=Adult → không SARGable |
| Gộp `Tour` + `TourSample` (TPT) | ✅ **GIỮ** | Autocomplete/ search chỉ query 1 bảng gốc `Tour`, không cần UNION |
| `TourCustomer` gộp vào Tour | ❌ **KHÔNG** | Quan hệ 1:N (vi phạm 1NF nếu gộp) |

### F3. Nguyên tắc denormalize CÓ CHỦ ĐÍCH

Schema SaaS **không** thuần normalized 3NF. Có chủ đích denormalize ở 3 chỗ để phục vụ grid:

1. **Cột tổng tài chính trên `Order`** (`TotalRevenue`, `TotalCost`, `ApprovedRevenue`…):
   giữ trực tiếp, đồng bộ **ở application code** (domain service tính lại trong cùng transaction
   khi ghi dòng con Receipt/Payment/OrderCost). **KHÔNG dùng DB trigger** (xem §I). Lý do: grid sort theo cột này.
2. **Cột giá chính trên `TourTemplateFields`** (`PriceAdult`): giữ trực tiếp. Lý do: grid
   mẫu sort theo giá người lớn.
3. **Cột lookup hiển thị** (vd `SellerName`, `CustomerName`): tùy chọn denormalize nếu
   grid thường join —权衡 với rủi ro dữ liệu lệch.

### F4. Giải pháp cho grid tổng hợp nặng → PostgreSQL Materialized View

Hệ cũ dùng stored proc với JOIN 10+ bảng + temp table (`#AllTour`, `#FilteredIds`) để
lọc + phân trang. Trong SaaS mới (EF Core, **KHÔNG stored proc**), không gọi proc được
mà vẫn phải chạy grid nhanh → dùng **Materialized View** của PostgreSQL:

```sql
-- Materialized View: snapshot grid "Tất cả chuyến" (refresh định kỳ / theo event)
CREATE MATERIALIZED VIEW mv_tour_departure_grid AS
SELECT
  t.TenantId, t.Id AS TourId, t.Code, t.Title, t.Status, t.DepartureDate,
  tdf.AmountAdults, tdf.AmountChildren,
  u.FullName AS AssignedToName,
  c.FullName AS MainCustomerName,
  COALESCE(o.TotalRevenue, 0) AS TotalRevenue,
  COALESCE(o.TotalCost, 0) AS TotalCost,
  COALESCE(o.ApprovedRevenue, 0) AS ApprovedRevenue,
  COUNT(DISTINCT tc.Id) AS CustomerCount
FROM Tours t
JOIN TourDepartureFields tdf ON tdf.TourId = t.Id
LEFT JOIN Users u ON u.Id = tdf.AssignedToUserId
LEFT JOIN Orders o ON o.TourDepartureId = t.Id
LEFT JOIN TourCustomers tc ON tc.TourDepartureId = t.Id
LEFT JOIN Customers c ON c.Id = (SELECT MIN(customer_id) FROM TourCustomers
                                  WHERE tour_departure_id = t.Id AND is_main_contact)
WHERE t.Kind = 'Departure' AND t.IsDeleted = false
GROUP BY t.Id, u.FullName, c.FullName, o.TotalRevenue, o.TotalCost, o.ApprovedRevenue;

CREATE UNIQUE INDEX ON mv_tour_departure_grid (TenantId, TourId);
CREATE INDEX ON mv_tour_departure_grid (TenantId, DepartureDate);
CREATE INDEX ON mv_tour_departure_grid (TenantId, TotalRevenue);
-- Refresh: REFRESH MATERIALIZED VIEW CONCURRENTLY mv_tour_departure_grid;
```

**Khi nào dùng:**
- Grid list với sort/filter theo cột tổng hợp (thu/chi, count khách, đánh giá).
- Báo cáo/dashboard.
- **KHÔNG** dùng cho chi tiết (1 bản ghi) — query thẳng bảng gốc.

**Refresh strategy (KHÔNG dùng trigger):**
- `REFRESH MATERIALIZED VIEW CONCURRENTLY` (không lock read) kích hoạt **từ application code**:
  sau khi ghi (Receipt/Payment/OrderCost/TourCustomer CRUD) → đẩy job vào **background job/queue** để refresh.
- Hoặc refresh định kỳ (mỗi 1-5 phút) nếu chấp nhận dữ liệu hơi stale.

### F5. Tại sao PostgreSQL (không phải SQL Server)

| Yêu cầu | PostgreSQL | SQL Server | SQLite (dev) |
|---|---|---|---|
| Materialized View | ✅ Native | ⚠️ Indexed view (ràng buộc khắt) | ❌ |
| JSONB (lưu Services, CheckList, payload) | ✅ Index được | JSON (khó index hơn) | TEXT |
| Open-source, self-host | ✅ Free | 💰 License | ✅ |
| EF Core provider | ✅ Nimma.EntityFrameworkCore | ✅ SqlServer | ✅ Sqlite |
| Full-text search (VD tên tour) | ✅ tsvector + GIN | ✅ Full-text | ⚠️ LIKE |
| CTE đệ quy (MarketType cây) | ✅ Tốt | ✅ | ⚠️ |

→ **Chốt: PostgreSQL production, SQLite dev.** Code provider-agnostic qua EF Core,
đổi `Database:Provider` trong config.

### F6. Checklist trước khi thêm/sửa bảng (bắt buộc)

Trước khi chốt schema 1 entity, trả lời 4 câu:
1. **Grid nào sẽ hiển thị entity này?** → sort/filter theo field nào? → field đó có index không?
2. **Có field tổng hợp (SUM/COUNT) cần sort không?** → denormalize lên bảng cha HOẶC dùng Materialized View.
3. **Có quan hệ 1:N không?** → KHÔNG gộp vào bảng cha (vi phạm 1NF). Tạo bảng con.
4. **Có cột dùng chung cho nhiều loại không?** → cân nhắc TPT (gốc + bảng phụ theo Kind).

---

## PHẦN G — ERD cụm Tour (Mermaid)

```mermaid
erDiagram
    Tour ||--o| TourTemplateFields : "Kind=Template"
    Tour ||--o| TourDepartureFields : "Kind=Departure"
    Tour ||--o{ TourItinerary : "lịch trình ngày"
    Tour ||--o{ TourAssignee : "người phụ trách/theo dõi"
    Tour ||--o{ PriceScenario : "giá theo đoàn (chỉ Template)"
    Tour ||--o{ TourCustomer : "khách trên chuyến (chỉ Departure)"
    Tour }o--|| MarketType : "thị trường"
    Tour ||--o| Tour : "ParentTourId (Departure→Template)"

    TourCustomer }o--|| Customer : ""
    TourCustomer }o--|| Order : ""

    Order ||--o{ OrderCost : "chi phí NCC"
    Order ||--o{ ReceiptVoucher : "thu"
    Order ||--o{ PaymentVoucher : "chi"

    Tour {
        Guid Id PK
        Guid TenantId
        Enum Kind "Template|Departure"
        string Code
        string Title
        int TourType
        date DepartureDate
        date EndDate
        int TotalSlots
        int Status
        Guid ParentTourId FK "NULL"
        Guid MarketId FK
    }
    TourTemplateFields {
        Guid TourId PK_FK
        decimal PriceAdult
        decimal PriceChild
        decimal PriceChildSmall
        decimal PriceBaby
        string CommissionAdultType
        decimal CommissionAdultValue
        int ReservationHours
        text TermsNote
    }
    TourDepartureFields {
        Guid TourId PK_FK
        int AmountAdults
        int AmountChildren
        Guid AssignedToUserId FK
        bool IsClosed
        date ClosedAt
        int CommissionStatus
        bool IsPaymentRecognized
    }
    TourCustomer {
        Guid Id PK
        Guid TourDepartureId FK
        Guid CustomerId FK
        Guid OrderId FK
        int Qty
        decimal PriceAdult
        decimal Surcharge
        decimal Discount
        decimal Commission
        string SeatCode
        bool IsMainContact
    }
    TourAssignee {
        Guid TourId FK
        Guid UserId FK
        Enum Role "Manager|Watcher|Assignee"
    }
    PriceScenario {
        Guid Id PK
        Guid TourTemplateId FK
        int FromQty
        int ToQty
        decimal UnitPrice
    }
```

**Tóm tắt cấu trúc:**
- `Tour` = bảng gốc (cột chung) + `Kind` phân loại.
- `TourTemplateFields` / `TourDepartureFields` = bảng phụ 1-1 theo Kind (TPT).
- `TourCustomer` = bảng riêng (1:N, không gộp).
- `TourAssignee` = bảng nối normalize `IdsNguoiTheoDoi`/`ManagerIds`.
- Grid mẫu → JOIN `Tour` + `TourTemplateFields`; grid chuyến → JOIN `Tour` + `TourDepartureFields` + Materialized View cho cột tổng.

---

## PHẦN H — Chiến lược Index cụ thể (theo read pattern §F)

> Nguyên tắc "tối ưu để sản phẩm dùng được": **mỗi cột dùng để sort/filter trên grid nóng phải nằm trong 1 index bắt đầu bằng `TenantId`**. Không index tràn lan (mỗi index thêm chi phí ghi) — chỉ index cái read pattern thật cần.

### H1. 4 quy tắc index
1. **TenantId đứng đầu mọi index** của `ITenantEntity` — vì query filter luôn `WHERE TenantId = @t` trước.
2. **Cột sort của grid** phải là cột tiếp theo trong composite index (để index vừa lọc tenant vừa phục vụ `ORDER BY` → tránh sort trên đĩa).
3. **Mọi FK** (`*Id`) có index riêng (join + cascade check).
4. **Unique theo tenant**: mã nghiệp vụ (`Code`) unique trong phạm vi tenant → `UNIQUE (TenantId, Code)`.

### H2. Index cụ thể cho các bảng nóng

| Bảng | Index (thứ tự cột) | Phục vụ read pattern |
|---|---|---|
| `Tour` | `(TenantId, Kind, Status)` · `(TenantId, DepartureDate)` · `UNIQUE(TenantId, Code)` · `(TenantId, ParentTourId)` · `(TenantId, MarketId)` | grid chuyến/mẫu lọc Kind+trạng thái, sort theo ngày khởi hành; autocomplete theo Code |
| `TourTemplateFields` | PK `(TourId)`; cân nhắc `(TourId) INCLUDE (PriceAdult)` | grid mẫu sort `PriceAdult` — xem H4 (cross-table sort) |
| `TourDepartureFields` | PK `(TourId)` · `(AssignedToUserId)` · `(TenantId, IsClosed)` | lọc chuyến theo người phụ trách / đã chốt |
| `Order` | `(TenantId, Status)` · `(TenantId, TourDepartureId)` · `(TenantId, CustomerId)` · `(TenantId, TotalRevenue DESC)` · `(TenantId, ApprovedRevenue DESC)` | grid sort theo cột tổng tài chính (denormalize, §A6) |
| `TourCustomer` | `(TenantId, TourDepartureId)` · `(TenantId, CustomerId)` · `(TenantId, OrderId)` · `(TenantId, ReservationCode)` | slot theo chuyến/khách/đơn; tra mã giữ chỗ |
| `Customer` | `UNIQUE(TenantId, Code)` · `(TenantId, Phone)` · `(TenantId, TypeId)` · GIN full-text `FullName` | tìm khách theo mã/sđt/loại; search tên (§H3) |
| `Lead` | `(TenantId, Status, AssignedToUserId)` · `(TenantId, RotationOrder)` | phân bổ round-robin, lọc lead theo sales |
| `ReceiptVoucher` / `PaymentVoucher` | `(TenantId, IssuedAt DESC)` · `(TenantId, Status)` · `(TenantId, OrderId)` · `UNIQUE(TenantId, Code)` · `(TenantId, ParentId)` | grid phiếu sort theo ngày/trạng thái; phiếu con theo cha |
| `Provider` | `(TenantId, Type, Status)` · `UNIQUE(TenantId, Code)` | lọc NCC theo loại |
| `ProviderServicePricing` | `(TenantId, ProviderServiceId, FromDate)` | tra giá hiệu lực theo mùa |
| `OrderCost` | `(TenantId, OrderId)` · `(TenantId, ProviderId)` | chi phí theo đơn/NCC |
| `User` | `UNIQUE(TenantId, Email)` · `(TenantId, DepartmentId)` | đăng nhập, lọc theo phòng ban |
| `ApprovalHistory` | `(TenantId, RefType, RefId, StepOrder)` | truy lịch sử duyệt của 1 phiếu |
| `AuditLog` | `(TenantId, EntityType, EntityId)` · `(TenantId, At DESC)` | tra vết theo entity/thời gian |

### H3. Partial index cho soft-delete + full-text (PostgreSQL)
- **Partial index** bỏ qua bản ghi đã xóa → index gọn, hợp với global filter `IsDeleted = false`:
  ```sql
  CREATE INDEX ix_tour_departure ON "Tours" ("TenantId","DepartureDate")
    WHERE "IsDeleted" = false;
  ```
- **Full-text tên** (tour/khách): `tsvector` + GIN thay `LIKE '%...%'` (LIKE dẫn đầu `%` không dùng được index):
  ```sql
  CREATE INDEX ix_customer_fts ON "Customers"
    USING GIN (to_tsvector('simple', "FullName"));
  ```

### H4. Vấn đề "sort theo cột bảng con" (cross-table) + hướng xử lý
Grid mẫu sort theo `PriceAdult` (nằm ở `TourTemplateFields`, join từ `Tour`). Index không thể phục vụ `ORDER BY` xuyên join. 3 mức xử lý theo tải:
1. **Nhỏ (≤ vài trăm mẫu/tenant):** sort trong bộ nhớ sau khi lọc tenant — chấp nhận được, không cần gì thêm.
2. **Vừa:** denormalize `PriceAdult` LÊN bảng gốc `Tour` (đồng bộ 1-1, cost thấp) để index `(TenantId, PriceAdult)`.
3. **Lớn / grid tổng hợp nhiều SUM:** **Materialized View** (§F4).

### H5. Ước lượng chi phí ghi
Mỗi index = +1 lần cập nhật B-tree mỗi khi ghi. Bảng ghi nóng (`TourCustomer`, `Order`, vouchers) → giữ số index ở mức cần thiết (bảng trên). Bảng đọc-nhiều-ghi-ít (`Customer`, `Provider`) → thoải mái index hơn.

---

## PHẦN I — Đồng bộ & toàn vẹn khi denormalize

> Denormalize (cột tổng trên `Order`, giá trên `TourTemplateFields`) đổi tốc độ đọc lấy rủi ro **lệch dữ liệu**. Bắt buộc có cơ chế giữ đồng bộ, nếu không "nhanh nhưng sai" còn tệ hơn.
>
> **Nguyên tắc (phiên bản mới):** đồng bộ **hoàn toàn ở application code** — **KHÔNG dùng DB trigger** (và không stored proc). Lý do: logic ẩn trong DB rất khó follow/trace/test; giữ toàn bộ ở C#/EF để một chỗ duy nhất quyết định, dễ đọc và unit-test.

1. **Nguồn sự thật = dòng con** (`ReceiptVoucher`/`PaymentVoucher`/`OrderCost`/`TourCustomer`). Cột tổng trên `Order` chỉ là bản sao tính sẵn.
2. **Tính lại trong CÙNG transaction** khi ghi dòng con: domain service (vd `OrderTotalsService.Recompute(orderId)`) chạy trong transaction ghi voucher/cost → không bao giờ commit lệch. **Đây là cơ chế chính** cho mọi provider (SQLite dev / SQL Server / PostgreSQL prod).
3. **KHÔNG dùng DB trigger** để tính lại tổng — cố ý loại bỏ để tránh logic ẩn khó follow. Mọi tính toán ở domain service (#2).
4. **Chống mất cập nhật (lost update):** `Order` có concurrency token (`xmin` PostgreSQL / `RowVersion`) → EF Core `IsRowVersion()` để optimistic locking khi 2 phiếu cùng cập nhật 1 đơn.
5. **Job đối soát định kỳ:** so cột tổng vs `SUM` dòng con, log lệch để phát hiện bug đồng bộ (chạy nền, ngoài giờ).
6. **Chỉ tính phiếu đã duyệt:** `Total_Thu/Chi` thực tế chỉ cộng phiếu `IsGhiNhanDongTien = 1 AND IsDeleted = false` (đúng logic hệ cũ — xem §J enum).

---

## PHẦN J — Catalog quyền & enum hệ cũ (đầu vào cho RBAC & trạng thái Phase 0b)

> Trích từ khảo sát module thật (vé máy bay, phiếu thu/chi, tour lẻ, tasking...). Dùng để **seed bảng `Permission`** và định nghĩa **enum trạng thái** cho hệ mới. Quy ước mã cũ: `MODULE_ĐỐITƯỢNG_HÀNHĐỘNG`.

### J1. Nhóm quyền quan sát được → map sang `Permission.Code` (RBAC)
| Nhóm cũ | Ví dụ mã cũ | Ý nghĩa | Đề xuất Code mới |
|---|---|---|---|
| Vé máy bay | `VMB_XEM(_ALL/_DH/_SALES)`, `VMB_TAOMOI`, `VMB_SUA`, `VMB_XOA`, `VMB_DUYET`, `VMB_EXPORTBILL`, `VMB_GROUP_XEM` | xem/tạo/sửa/xóa/duyệt/xuất bill vé | `airticket.view/create/update/delete/approve/export` |
| Tour lẻ | `TR_TL_XEM(_DH)`, `TR_TL_SUA`, `TR_TL_XOA`, `TR_TL_CT`, `TR_TL_HT_KH` | xem/sửa/xóa/chi tiết tour lẻ | `tour.single.view/update/delete/detail` |
| Xác nhận chỗ | `TR_TM_XNC` | quyền "Xác nhận chỗ" (giữ chỗ→xác nhận) | `booking.seat.confirm` |
| Phiếu thu | `KT_PT_XEM(_ALL)`, `KT_PT_DUYET`, `KT_PT_CHON_DUYET`, `KT_PT_SUA`, `KT_PT_XOA`, `KT_PT_CHUYEN`, `KT_PT_TAOMOI_DH`, `KT_PT_EXPORTBILL`, `KT_PT_BILLTOUR` | thu tiền | `finance.receipt.*` |
| Phiếu chi | `KT_PC_XEM(_ALL)`, `KT_PC_DUYET`, `KT_PC_CHON_DUYET`, `KT_PC_SUA`, `KT_PC_XOA`, `KT_PC_TAOMOI_DH` | chi tiền | `finance.payment.*` |
| Nhà cung cấp | `NC_NC_XEM(_GIA_NHAP/_GIA_BAN)`, `NC_NC_LN`, `NC_NC_MAIL` | xem NCC + giá nhập/bán/lợi nhuận | `provider.view / provider.price.buy/sell / provider.profit` |
| Hoa hồng | `HH_CHOT` | chốt hoa hồng | `commission.close` |
| Báo cáo | `BC_NV_XEM`, `BC_TC_XEM` | báo cáo nhân viên/tài chính | `report.staff/finance.view` |
| Công việc | `CV_XEM_ALL`, `CV_TAOMOI`, `CV_SUA`, `CV_XOA`, `DA_XEM` | task/dự án nội bộ | `task.* / project.view` |
| **Data-scope** (cắt phạm vi) | `CN_CN_XEM` (chi nhánh), `NHOM_XEM_ALL` (nhóm), `*_XEM_ALL` (toàn bộ) | **KHÔNG là quyền hành động** mà là phạm vi dữ liệu | → cơ chế **data scope** riêng (Own/Department/Branch/All), không nhét vào Permission |

> **Bài học cho RBAC mới:** tách rõ 2 trục — **(a) quyền hành động** (`view/create/update/delete/approve/export`) và **(b) phạm vi dữ liệu** (`Own | Department | Branch | Tenant`). Hệ cũ trộn chung (`_XEM` vs `_XEM_ALL`, `CN_CN_XEM`) gây bùng nổ mã quyền. Thiết kế Phase 0b nên: `Permission = resource.action`, còn scope gắn ở `UserRole`/policy.

### J2. Enum trạng thái thật (giữ ngữ nghĩa, đổi tên PascalCase)
| Enum cũ | Giá trị | Dùng cho |
|---|---|---|
| `State` (record chung) | Hidden=0, Created=1, Active=2, UnActive=3 (chờ duyệt), Delete=4, Off=5 | vòng đời bản ghi (thay bằng `IsDeleted` + `Status` riêng) |
| `TourState` | Pendding=101, Happenning=102, Completed=103, Canceled=104, Baogia=106, Abort=107, No_Settlement=108 | trạng thái chuyến/đơn |
| `BookingType` | AllTour=0, SingleTour=1, Series=2, GroupTour=3, PaymentVoucher=4, ReceiptVoucher=5, CarManagement=9 | phân loại đơn/tour |
| Duyệt phiếu (phân cấp) | `InProgress → Approved / Rejected`; bước: `Pending → Approved/Rejected`; method `One | All`; hỗ trợ **rewind** (reject bước giữa lùi 1 bước) + **recall** (thu hồi) | engine `ApprovalProcess` dùng chung |
| Ghi nhận tiền | `IsGhiNhanDongTien`: null=chờ duyệt, 1=đã duyệt (tính công nợ), 0=không duyệt | phiếu thu/chi |

### J3. Trạng thái "Giữ chỗ" (Booking core) — chuẩn hóa từ flow thật
Từ `docs/flow-giu-cho`: trạng thái 1 chỗ (`TourCustomer`) suy ra từ tiền, không lưu enum cứng:
| Điều kiện | Trạng thái | Ghi chú |
|---|---|---|
| `UpfrontAmount == 0` | **Giữ chỗ** | có countdown hết hạn (`ReservationHours`) |
| `UpfrontAmount == 0` & hết countdown & chốt | **Chốt chỗ (không nhả)** | |
| `0 < UpfrontAmount < PricePerSlot` | **Đã đặt cọc** | |
| `UpfrontAmount >= PricePerSlot` | **Đã thanh toán** | |
| `StatusCancel != 0` | **Đã huỷ** | kèm ngày huỷ |

→ Hệ mới nên lưu `SeatStatus` tường minh (Held/Deposited/Paid/Cancelled) + `HoldExpiresAt`, thay vì suy ra từ tiền mỗi lần render (đỡ tính lại, index được để lọc "sắp hết hạn giữ chỗ").

---

## PHẦN K — Phân tích chuẩn hóa 3NF

> Mục tiêu: chuẩn hóa để **không trùng lặp / không lệch dữ liệu** (OLTP đúng), nhưng **denormalize có kiểm soát** đúng điểm nóng đọc (§F). "Không tối ưu" xảy ra ở **cả 2 thái cực**: 3NF thuần 100% → grid chậm; denormalize bừa → dữ liệu lệch. Nguyên tắc: **3NF là mặc định, denormalize là ngoại lệ có lý do + có cơ chế giữ đúng.**

### K1. Nhắc lại 3 dạng chuẩn (áp cho schema §B)
- **1NF** — giá trị nguyên tử, không "mảng trong cột", không nhóm lặp.
- **2NF** — 1NF + không phụ thuộc bộ phận vào *một phần* khóa tổ hợp.
- **3NF** — 2NF + không phụ thuộc bắc cầu (non-key phụ thuộc non-key khác, không phải khóa).

### K2. Vi phạm 1NF của hệ cũ → đã đưa về chuẩn
| Hệ cũ (vi phạm 1NF) | Kiểu vi phạm | Sửa (§A3) |
|---|---|---|
| `Customer.IdsNguoiTheoDoi`, `Tour.ManagerIds` (CSV id) | nhóm lặp trong cột | bảng nối `CustomerWatcher`, `TourAssignee` |
| `Customer.tagCustomer` (CSV) | nhóm lặp | `CustomerTag(CustomerId, TagId)` |
| `Tour.marketId` (CSV) | đa trị | FK `MarketId` + `MarketType` |
→ Sau chuẩn hóa: **join được, index được, không lệch**. ✅ 1NF.

### K3. 2NF — gần như "miễn phí" nhờ surrogate key
- Mọi bảng nghiệp vụ dùng **khóa thay thế `Id (Guid)` đơn cột** → non-key phụ thuộc toàn bộ khóa (1 cột) → **không thể có phụ thuộc bộ phận** → tự động 2NF.
- Bảng nối (`RolePermission`, `UserRole`, `CustomerTag`) khóa tổ hợp nhưng **chỉ chứa khóa** (không có thuộc tính non-key) → 2NF OK.
- ⚠️ Nếu bảng nối cần thuộc tính riêng (vd `UserRole` thêm `AssignedAt`) → thuộc tính đó phụ thuộc **cả cặp** (User+Role) nên vẫn 2NF; chỉ sai nếu vô tình thêm cột chỉ phụ thuộc `UserId`.

### K4. 3NF — điểm cần canh: phụ thuộc bắc cầu
**Quy tắc lookup (giữ 3NF):** bảng nghiệp vụ lưu **FK Id**, KHÔNG lưu tên hiển thị.
- `Customer.TypeId` (không lưu `TypeName`) → `TypeName` chỉ ở `CustomerType`, tra qua join.
- `Order.CustomerId` (không lưu `CustomerName`), `Tour...AssignedToUserId` (không lưu `SellerName`).

> ❌ **Cạm bẫy 3NF hay gặp:** lưu `SellerName`/`CustomerName`/`ProviderName` thẳng trên bảng con để grid khỏi join. Đây là **phụ thuộc bắc cầu** (Name phụ thuộc FK, không phụ thuộc khóa của bảng con) → đổi tên 1 chỗ là **lệch toàn bộ bản ghi cũ**. Cấm lưu ở bảng OLTP. Nếu grid cần hiển thị tên mà không muốn join nặng → đưa vào **Materialized View** (§F4): tên nằm ở *view snapshot*, không phải nguồn sự thật, refresh lại là đúng.

### K5. Ba chỗ cố tình lệch chuẩn (denormalize có kiểm soát) — phân biệt rõ
| Chỗ | Có thực sự vi phạm 3NF? | Xử lý |
|---|---|---|
| **Cột tổng trên `Order`** (`TotalRevenue`, `ApprovedRevenue`…) | **CÓ** — là giá trị *dẫn xuất* (`SUM` dòng con) nên phụ thuộc dữ liệu bảng khác, không phải thuộc tính độc lập của `Order` | Chấp nhận vì grid sort trực tiếp (§A6). Giữ đúng bằng domain service trong transaction (KHÔNG trigger) + optimistic lock + job đối soát (**§I**) |
| **4 cột giá theo tuổi** (`PriceAdult/Child/…`) trên `TourTemplateFields` | **KHÔNG** — mỗi giá là **thuộc tính nguyên tử độc lập** của template, không phụ thuộc thuộc tính non-key nào khác. Chỉ là "cột song song", không phải bắc cầu | Vẫn ở 3NF; giữ cột (không tách `PriceTier`) vì grid sort theo `PriceAdult` (§A7). Mở rộng hiếm → `ExtraPricing jsonb` |
| **Cột tên trong Materialized View** (`SellerName`, `MainCustomerName`) | Bắc cầu — **nhưng nằm ở VIEW**, không ở bảng nguồn | Hợp lệ: MV là snapshot đọc, `REFRESH` đồng bộ lại (§F4) |

### K6. Bảng verdict 3NF cho các entity chính
| Entity | 1NF | 2NF | 3NF | Ghi chú |
|---|---|---|---|---|
| `Customer`, `Lead`, `Provider`, `User` | ✅ | ✅ | ✅ | chỉ lưu FK lookup, không lưu tên |
| `Tour` / `TourTemplateFields` / `TourDepartureFields` | ✅ | ✅ | ✅ | 4 cột giá = thuộc tính độc lập (K5) |
| `TourCustomer` | ✅ | ✅ | ✅ | giá/phụ thu/CK theo tuổi = thuộc tính độc lập; có surrogate Id |
| `Order` | ✅ | ✅ | ⚠️ **cố ý** | cột tổng dẫn xuất — ngoại lệ có kiểm soát (§I) |
| `OrderCost`, `ReceiptVoucher`, `PaymentVoucher` | ✅ | ✅ | ✅ | dòng chi tiết = nguồn sự thật |
| Bảng nối (`*Tag`, `*Assignee`, `RolePermission`, `UserRole`) | ✅ | ✅ | ✅ | chỉ chứa khóa (+thuộc tính phụ thuộc cả cặp nếu có) |
| `AuditLog` | ✅ | ✅ | ✅* | `Before/After` là `jsonb` snapshot — chấp nhận (bản chất log, không phải OLTP quan hệ) |

### K7. Checklist 3NF khi thêm bảng mới (ghép với §F6)
1. Có cột nào chứa **danh sách/CSV**? → tách bảng nối (1NF).
2. Có cột nào là **tên/nhãn của một FK** (SellerName theo UserId)? → **bỏ**, join hoặc đưa vào MV (3NF).
3. Có cột nào là **tổng/đếm dẫn xuất**? → mặc định KHÔNG lưu; chỉ denormalize nếu grid sort theo nó, kèm cơ chế §I.
4. Bảng nối có thuộc tính riêng? → xác nhận nó phụ thuộc **toàn bộ** khóa tổ hợp (2NF).

---

## PHẦN L — 3NF + JSONB: chuẩn hóa lai (hybrid)

> Ý tưởng: **chuẩn hóa 3NF cho phần "lõi quan hệ"** (cái cần query/filter/join/aggregate) + **jsonb cho phần "đuôi dài" bán cấu trúc** (cái chỉ đọc/ghi nguyên khối). Tránh 2 lỗi ngược nhau: (a) over-normalize → hàng chục bảng con lèo tèo + cột nullable (EAV); (b) nhồi tất cả vào JSON → mất index, mất toàn vẹn, mất khả năng báo cáo.

### L1. jsonb đứng ở đâu so với 3NF
- Về lý thuyết quan hệ, một cột jsonb là **giá trị không nguyên tử** → "lệch 1NF". Nhưng nếu ta **luôn xử lý cả khối như một giá trị đục** (đọc/ghi nguyên cục, không filter theo field con) thì chấp nhận được — giống lưu 1 tài liệu.
- Ranh giới quyết định = **"có cần truy vấn theo field bên trong không?"**
  - CÓ (sort/filter/join/SUM theo field đó) → **cột chuẩn hóa** (3NF).
  - KHÔNG (chỉ đọc/ghi nguyên khối) → **jsonb** trên chính hàng đó.

### L2. Bảng quyết định "jsonb hay chuẩn hóa" cho TourKit
| Dữ liệu | Chọn | Lý do |
|---|---|---|
| `Plan.Features` (cờ tính năng theo gói) | **jsonb** | shape thay đổi theo gói, đọc nguyên khối, không filter |
| `TourTemplateFields.ExtraPricing` (nhóm tuổi mở rộng hiếm) | **jsonb** | tràn ngoài 4 nhóm cố định; hiếm → không đáng thêm bảng |
| `TourDepartureFields.CheckList` (danh mục việc điều hành) | **jsonb** | list item đọc/ghi nguyên khối |
| `Tour.Services` (dịch vụ kèm — chỉ hiển thị) | **jsonb** | nếu chỉ hiển thị. **Nhưng** nếu có tính chi phí/đối chiếu NCC → chuẩn hóa `OrderCost`/`ProviderService` |
| `CommissionCampaign.RuleJson` (luật hoa hồng) | **jsonb** | engine diễn giải; luật đa dạng, không cột hóa được |
| `AuditLog.Before/After` (snapshot) | **jsonb** | bản chất là ảnh chụp, không query field con |
| `BackgroundJob.Payload` | **jsonb** | tham số job đục |
| `SettingPage` / `UserGridSettings` (UI config per-user) | **jsonb** | cấu hình co giãn, per-user |
| `Notification.Meta` (dữ liệu theo loại thông báo) | **jsonb** | shape khác nhau theo `Type` |
| **Giá 4 nhóm tuổi** (`PriceAdult`…) | **cột** | grid sort theo `PriceAdult` (§A7) — jsonb sẽ không SARGable |
| **Cột tổng `Order`** | **cột** | grid sort + báo cáo SUM (§A6) |
| **`TourCustomer` từng chỗ** | **hàng (bảng)** | quan hệ 1:N, join + aggregate |
| **Tag khách** | **bảng nối `CustomerTag`** *nếu* hay lọc "khách theo tag" (join/count). Nếu tag chỉ là nhãn hiển thị hiếm lọc → có thể `jsonb array` + GIN | quyết định theo read pattern |

### L3. Index cho jsonb (PostgreSQL)
- **GIN** cho truy vấn chứa (`@>`) / tồn tại key: `CREATE INDEX ... USING GIN (data jsonb_path_ops);`
- **Expression index** nếu thỉnh thoảng filter theo 1 path cố định:
  `CREATE INDEX ... ((data->>'status'));`
- ⚠️ **Tín hiệu cảnh báo:** nếu phải index nhiều path trong jsonb → đó là dấu hiệu field đó **nên là cột chuẩn hóa**, không phải json.

### L4. Cái giá của jsonb (đánh đổi phải biết)
1. **Không có FK** vào trong json → mất toàn vẹn tham chiếu (đừng cất `CustomerId` cần ràng buộc vào json).
2. **Không schema enforcement** → phải **validate ở tầng app** (EF Core value converter / owned type + FluentValidation). Rủi ro "rác" nếu buông lỏng.
3. **Khó migrate** khi đổi shape (không có ALTER COLUMN cho field con) → viết migration dữ liệu thủ công.
4. **Không SUM/GROUP BY tự nhiên** → không dùng cho số liệu cần báo cáo.
5. **Ghi đè cả khối** → cập nhật 1 field vẫn rewrite cả json (chú ý với blob lớn, ghi nóng).

### L5. Ánh xạ EF Core (net9/EF Core 9)
- **Owned type + `ToJson()`**: map 1 object C# vào 1 cột jsonb, vẫn code typed:
  ```csharp
  modelBuilder.Entity<Plan>().OwnsOne(p => p.Features, b => b.ToJson());
  // hoặc list:
  modelBuilder.Entity<TourDeparture>().OwnsMany(t => t.CheckList, b => b.ToJson());
  ```
- Hoặc **value converter** cho blob đục (`Dictionary`/`JsonDocument`).
- Provider-aware: PostgreSQL → cột `jsonb` (Npgsql); SQLite dev → lưu `TEXT` (cùng converter, chạy được, chỉ mất GIN — chấp nhận ở dev).

### L6. Nguyên tắc chốt (ghép §F6 + §K7)
> **Lõi quan hệ (query/filter/join/sum) → cột 3NF. Đuôi dài bán cấu trúc (đọc nguyên khối) → jsonb. jsonb KHÔNG phải bãi rác cho lười chuẩn hóa — mỗi lần định cho field vào jsonb, hỏi: "có bao giờ cần lọc/sort/sum theo nó không?" Nếu có → cột.**
