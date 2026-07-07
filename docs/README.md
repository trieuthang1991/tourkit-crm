# TourKit — Điểm bắt đầu (đọc file này trước)

> Nền tảng SaaS điều hành tour đa doanh nghiệp, viết lại greenfield từ hệ cũ (`script.sql`, 144 bảng, đã phân mảnh).
> Cập nhật lần cuối: 2026-07-08.

## Trạng thái hiện tại

Solution `.NET 9` + EF Core 9 chạy trên **SQLite** (dev), 23 test xanh. Đã xong:

| Phase | Nội dung | Trạng thái |
|---|---|---|
| 0a | Kernel multi-tenant (query filter + soft-delete + interceptor) | ✅ |
| 0b-1 | Identity + JWT Auth (login/refresh, tenant từ claim) | ✅ |
| 1 | Catalog: Tour TPT (Template/Departure) + Itinerary + CRUD TourTemplate | ✅ |
| 0b-2 | RBAC: Permission (global) + Role/RolePermission/UserRole, quyền vào JWT, gate endpoint | ✅ |

**Việc tiếp theo:** 0b-3 provisioning tenant · 0b-4 subscription · 1b (PriceScenario/assignee/MarketType/Departure) · 2 CRM · 3 Booking · 4 Provider · 5 Finance. Plan đã có: `plans/2026-07-07-phase0b1-*`, `phase1-catalog-tour`, `phase0b2-rbac`.

> **Lưu ý điều chỉnh so với thiết kế gốc:** dùng **.NET 9** (thay vì 10) và **SQLite ở dev** (SQL Server để production) theo yêu cầu môi trường máy hiện tại. Provider đổi bằng cấu hình, không sửa code.

## Đọc theo thứ tự

1. **Thiết kế tổng thể** → `docs/superpowers/specs/2026-07-07-tourkit-saas-platform-design.md`
   - Toàn bộ quyết định kiến trúc, phạm vi MVP, lộ trình 5 phase.
2. **Convention (BẮT BUỘC tuân thủ khi code)**
   - Backend → `docs/conventions/backend-conventions.md`
   - Frontend → `docs/conventions/frontend-conventions.md`
3. **Kế hoạch code tiếp theo** → `docs/superpowers/plans/2026-07-07-phase0a-multitenant-foundation.md`
   - 7 task chi tiết theo TDD, có sẵn code + lệnh.

## Các quyết định đã chốt

| Hạng mục | Quyết định |
|----------|-----------|
| Mô hình | SaaS thương mại bán cho nhiều công ty lữ hành |
| Tech backend | **.NET 9 + EF Core 9**, logic ở code (KHÔNG stored proc) |
| Database | **SQLite ở dev** (nhanh khi bootstrap) → **PostgreSQL ở production**. Đổi bằng cấu hình `Database:Provider`; code provider-agnostic. Chọn PostgreSQL vì open-source, linh hoạt, có Materialized View + JSONB để phục vụ các màn grid tổng hợp (xem `docs/business/database-optimization-analysis.md` §F) |
| Kiến trúc | Modular Monolith |
| Multi-tenant | Shared DB + `TenantId` + EF Global Query Filter (kèm soft-delete filter) |
| API | REST, prefix `/api/v1`, DTO record riêng, lỗi trả ProblemDetails |
| Frontend | React + TypeScript (strict) + Vite + **Ant Design** + TanStack Query |
| Đặt tên DB | PascalCase (chuẩn EF Core) |
| Dữ liệu cũ | Khởi đầu trống ở v1; import để v2 |

## MVP (bản bán được) — 5 khối
1. Nền móng multi-tenant + Auth/RBAC
2. Catalog: Tour · Lịch trình · Bảng giá · Mở chuyến
3. Booking: Khách hàng · Đơn · Ghế
4. Finance gọn: Phiếu thu · Công nợ khách
5. Billing/gói + onboarding + dashboard tối thiểu

## VIỆC CẦN LÀM TIẾP (khi quay lại)

- [x] **Phase 0a** — nền móng multi-tenant (net9 + EF Core 9 + SQLite). Xong: kernel tenancy, global query filter + soft-delete, SaveChanges interceptor, REST `/api/v1/customers`, migration SQLite, 5 test cô lập xanh.
- [ ] **Chạy API thử:** `dotnet run --project src/TourKit.Api` rồi gọi `POST /api/v1/customers` kèm header `X-Tenant-Id: <guid>`.
- [ ] **Plan Phase 0b** (JWT auth, RBAC/permission, đăng ký & provisioning tenant, subscription/plan, khung React + đăng nhập). *Chưa viết — nhờ tạo khi tới đó.*
- [ ] **Khi có SQL Server:** đặt `Database:Provider=SqlServer` + connection string, rồi tạo bộ migration SQL Server riêng.

## Môi trường đã kiểm tra (máy này)
- .NET SDK 9.0.300 + 10.0.100 ✅ · runtime .NET 9.0.5 ✅ · Node v22.15 ✅ · `dotnet-ef` đã cài ✅
- DB dev: **SQLite** (file `src/TourKit.Api/TourKit_Dev.db`, tự tạo, đã gitignore). Chưa cần SQL Server.

## Cấu trúc repo hiện tại
```
TourKit/
├─ TourKit.sln
├─ script.sql                  # DB hệ cũ (tham chiếu nghiệp vụ, KHÔNG dùng lại schema)
├─ .editorconfig               # ép style/naming
├─ Directory.Build.props       # net9 + nullable + warnings-as-errors + analyzers
├─ src/
│  ├─ TourKit.Shared/          # kernel: BaseEntity, ITenantEntity, ITenantContext
│  ├─ TourKit.Infrastructure/  # AppDbContext (filter+interceptor), entities, configs, migrations
│  └─ TourKit.Api/             # composition root + REST endpoints /api/v1
├─ tests/
│  └─ TourKit.Tests/           # test cô lập tenant (đọc/ghi/HTTP)
└─ docs/
   ├─ README.md · conventions/ · superpowers/{specs,plans}/
```
