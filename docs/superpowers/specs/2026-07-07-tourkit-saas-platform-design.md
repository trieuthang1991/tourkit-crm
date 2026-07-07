# TourKit — Nền tảng SaaS điều hành tour đa doanh nghiệp

**Ngày:** 2026-07-07
**Trạng thái:** Draft — chờ review
**Tác giả:** thangtv@xmedia.vn (với Claude Code)

---

## 1. Bối cảnh & Vấn đề

Hệ thống hiện tại (`DemoStaging`) là một ERP điều hành tour viết trên **.NET Framework + Entity Framework 6 + SQL Server**: 144 bảng, 42 table types, 61 stored procedures.

**Điểm đau (đã xác nhận qua phân tích schema):**

- **Version sprawl:** logic nghiệp vụ nằm trong stored procedure → mỗi lần đổi nghiệp vụ lại đẻ ra table type + proc mới thay vì migrate. Ví dụ: `booking_list_tbltype` có V3, V4, V4_5, V5, v8, v9, v10, v11; `provider_list_tbltype` v5→v10; `Order_Chi_tbltype` v2→v4. Không thể refactor an toàn.
- **Trùng khái niệm:** `ActivityLogs` + `ActivityLogsNewVersion`, nhiều bảng comment/log rời rạc.
- **Thiếu convention:** trộn English + tiếng Việt không dấu (`PhongBan`, `LoaiDonHang`, `TrangThaiDonHang`, `LichSuTheoDoi`).
- **Single-tenant:** 0 cột `tenant_id`/`company_id` — không thể phục vụ nhiều doanh nghiệp.

## 2. Mục tiêu

Xây **lại từ đầu (greenfield)** một **SaaS thương mại đa tenant** để nhiều công ty lữ hành đăng ký sử dụng chung một nền tảng, dữ liệu cô lập theo từng công ty, tính phí theo gói.

**Không mục tiêu (Non-goals) cho v1:**
- Migrate khối dữ liệu single-tenant cũ (để lại công cụ import ở v2).
- Tài chính nâng cao (chia lợi nhuận, hoa hồng nhiều tầng, duyệt nhiều cấp).
- Marketing (Email/SMS/Zalo), KPI, Workflow/Tasking.

## 3. Quyết định kiến trúc (đã chốt)

| # | Quyết định | Lựa chọn |
|---|-----------|----------|
| 1 | Mô hình sản phẩm | SaaS thương mại bán cho nhiều công ty lữ hành |
| 2 | Tech stack | .NET 8/9 + EF Core + SQL Server; **logic ở code layer, KHÔNG stored proc** |
| 3 | Tenant isolation | **Shared DB + `tenant_id`** trên mọi bảng nghiệp vụ; EF Core Global Query Filter |
| 4 | Frontend | **API-first Web API + React SPA** (khuyến nghị; có thể thay Blazor sau, không đụng backend) |
| 5 | Kiến trúc backend | **Modular Monolith** (mô-đun hóa theo bounded context, deploy 1 khối) |

## 4. Kiến trúc tổng thể

**Modular Monolith** — một solution .NET chia thành các module độc lập theo bounded context, giao tiếp qua interface rõ ràng. Chọn monolith (không microservices) vì: team nhỏ, MVP cần tốc độ, dễ vận hành. Ranh giới module rõ ràng để sau này có thể tách service nếu cần.

```
TourKit.sln
├─ src/
│  ├─ TourKit.Api            (ASP.NET Core Web API — composition root, controllers)
│  ├─ TourKit.Shared         (kernel: Result, tenant context, base entity, exceptions)
│  ├─ TourKit.Tenancy        (đăng ký tenant, resolve tenant, provisioning, subscription)
│  ├─ TourKit.Identity       (users, roles, permission RBAC theo tenant, JWT)
│  ├─ TourKit.Catalog        (tours, tour_samples, itineraries, departures, pricing)
│  ├─ TourKit.Booking        (orders, booking tickets, seats/ghế, customers)
│  ├─ TourKit.Finance        (receipt/payment vouchers, công nợ khách — bản gọn)
│  ├─ TourKit.Reporting      (dashboard queries — read model)
│  └─ TourKit.Infrastructure (EF Core DbContext, migrations, file storage, jobs, email)
└─ web/  (React SPA: Vite + TypeScript + data grid)
```

**Phân lớp mỗi module:** Domain (entity + business rule) → Application (use case/service, DTO) → Infrastructure (EF config, repo nếu cần). Controllers ở `TourKit.Api` mỏng, chỉ điều phối.

## 5. Thiết kế Multi-tenancy (nền tảng)

- **`ITenantContext`** (scoped): giữ `TenantId` của request hiện tại, resolve từ JWT claim (mỗi user thuộc 1 tenant) — không tin header từ client.
- **`ITenantEntity`**: mọi entity nghiệp vụ implement, có `TenantId`.
- **EF Core Global Query Filter:** `modelBuilder.Entity<T>().HasQueryFilter(e => e.TenantId == _tenantContext.TenantId)` áp tự động cho mọi entity tenant → mọi query đọc bị lọc, không thể quên.
- **Auto-set khi ghi:** override `SaveChanges` gán `TenantId` cho entity mới → không thể quên khi insert.
- **Bảo vệ nhiều lớp:** (1) global filter, (2) integration test bắt buộc kiểm tra cô lập chéo tenant cho mỗi module, (3) index bắt đầu bằng `TenantId`.
- **Bảng hệ thống KHÔNG có tenant:** `Tenants`, `Subscriptions`, `Plans`, global config.
- **Provisioning:** đăng ký công ty mới → tạo record `Tenant`, seed role mặc định, tạo user admin đầu tiên, gán gói dùng thử.

## 6. Mô hình miền cho MVP (schema sạch)

Đặt tên: **English, snake_case, số ít cho bảng** (hoặc theo convention EF Core PascalCase — chốt ở bước plan). Mọi bảng nghiệp vụ có `tenant_id`, `id` (guid/bigint), `created_at`, `updated_at`, `created_by`, `is_deleted` (soft delete).

**Identity & Tenancy**
- `tenant` (công ty), `subscription`, `plan`
- `user`, `role`, `permission`, `role_permission`, `user_role`

**Catalog (Tour)**
- `tour` (sản phẩm mẫu), `tour_itinerary` (ngày/chặng), `tour_price` (bảng giá theo loại khách: người lớn/trẻ em/em bé)
- `departure` (chuyến mở bán: ngày đi/về, sức chứa, trạng thái)

**Booking**
- `customer`
- `booking` (đơn, gắn `departure_id`, `customer_id`, tổng tiền, trạng thái)
- `booking_pax` (từng khách trong đơn: loại khách, ghế, phụ thu, giảm giá)
- `seat_hold` / cancel logic (giữ/hủy ghế)

**Finance (gọn)**
- `receipt_voucher` (phiếu thu tiền cọc/thanh toán, gắn `booking_id`)
- `payment_method`
- Công nợ khách = tính từ (tổng đơn − tổng đã thu), không cần bảng riêng ở v1.

**Cross-cutting**
- `activity_log` (audit, một bảng duy nhất — thay cho `ActivityLogs`+`ActivityLogsNewVersion`)
- `file_upload`

## 7. Cải tiến chính so với hệ cũ

| Vấn đề cũ | Cách làm mới |
|-----------|-------------|
| Logic trong 61 stored proc | Logic ở Application/Domain layer, test được bằng unit test |
| Version sprawl (v3→v11 table types) | Không table type; thay đổi schema = EF Core migration versioned trong git |
| Trùng bảng (`...NewVersion`) | Một bảng chuẩn + soft delete + audit log |
| Naming lộn xộn VN/EN | Một convention English nhất quán |
| Single-tenant | `tenant_id` + global query filter từ nền móng |

## 8. Cross-cutting concerns

- **Auth:** JWT (access + refresh), mật khẩu hash (ASP.NET Identity hoặc tương đương). Claim chứa `tenant_id`, `user_id`, roles.
- **Phân quyền:** RBAC theo tenant — permission dạng `resource.action` (vd `booking.create`), gán vào role, role gán cho user. Kế thừa ý tưởng `FunctionQuyen/SubjectQuyen` cũ nhưng chuẩn hóa.
- **Audit log:** ghi tự động ở `SaveChanges` (ai, làm gì, entity nào, trước/sau).
- **Background jobs:** Hangfire (gửi email onboarding, tính toán nền) — hợp hệ .NET.
- **File storage:** abstraction `IFileStorage` (local dev → S3/Azure Blob prod).
- **Migration schema:** EF Core Migrations, chạy tự động khi deploy.

## 9. Billing / Subscription (SaaS)

- `plan` (gói: giới hạn user, số booking/tháng, module bật/tắt), `subscription` (tenant ↔ plan, trạng thái, chu kỳ), dùng thử N ngày.
- v1: quản lý gói + khóa tính năng theo gói (feature flag theo plan). Tích hợp cổng thanh toán (VNPay/Stripe) để v1.5.

## 10. Onboarding tenant mới

Đăng ký → tạo tenant + admin → wizard thiết lập: thông tin công ty, tạo tour mẫu đầu tiên, mời thành viên. Seed dữ liệu mẫu tùy chọn để công ty làm quen.

## 11. Lộ trình (phases)

- **Phase 0 — Nền móng:** solution, DbContext, tenancy (global filter + provisioning), Identity/RBAC, đăng ký/đăng nhập, khung React + auth. *(Điều kiện: cô lập tenant có test xanh.)*
- **Phase 1 — Catalog:** tour, itinerary, pricing, departure + màn quản lý.
- **Phase 2 — Booking:** customer, booking, pax, ghế/hủy ghế + màn nhận đơn.
- **Phase 3 — Finance gọn:** phiếu thu, công nợ khách + dashboard tối thiểu.
- **Phase 4 — Billing/Plan + onboarding wizard** → bản bán được (v1).
- **v2+:** Nhà cung cấp & công nợ NCC, tài chính nâng cao (hoa hồng, chia lợi nhuận, duyệt), Marketing, KPI, công cụ import dữ liệu cũ.

## 12. Rủi ro & giảm thiểu

| Rủi ro | Giảm thiểu |
|--------|-----------|
| Lọt tenant filter → lộ dữ liệu chéo | Global filter + auto-set + integration test cô lập bắt buộc mỗi module |
| Nghiệp vụ tour VN phức tạp (phụ thu, trẻ em/em bé, ghế) | Lấy quy tắc từ schema cũ làm chuẩn nghiệp vụ; xác nhận với 1 công ty pilot |
| Scope phình to (144 bảng) | Bám sát MVP 5 khối; mọi thứ khác đẩy v2 |
| Thiếu người React | Backend API-first cho phép chuyển Blazor mà không sửa backend |

## 13. Quyết định đã chốt (2026-07-07)

1. **Frontend:** ✅ **React SPA** trên backend API-first.
2. **Dữ liệu cũ:** ✅ **Khởi đầu trống** cho v1; công cụ import dữ liệu cũ đẩy sang v2.
3. **Convention đặt tên DB:** ✅ **PascalCase** (chuẩn EF Core mặc định, ít ma sát). Bảng số nhiều theo mặc định EF (`Tours`, `Bookings`...).
4. **Công ty pilot:** ⏳ Chưa có — xác thực nghiệp vụ dựa trên quy tắc trích từ schema cũ; cập nhật khi có công ty pilot.

---

*Bước tiếp theo sau khi duyệt spec này: chuyển sang `writing-plans` để lập kế hoạch triển khai chi tiết cho Phase 0 (nền móng multi-tenant).*
