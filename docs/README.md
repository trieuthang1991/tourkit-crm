# TourKit — Điểm bắt đầu (đọc file này trước)

> Nền tảng SaaS điều hành tour đa doanh nghiệp, viết lại greenfield từ hệ cũ (`script.sql`, 144 bảng, đã phân mảnh).
> Cập nhật lần cuối: 2026-07-07.

## Trạng thái hiện tại

Đã xong phần **thiết kế + convention + kế hoạch**. **Chưa viết code.** Sẵn sàng để bắt đầu code Phase 0a.

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
| Tech backend | .NET 10 + EF Core 10 + SQL Server, logic ở code (KHÔNG stored proc) |
| Kiến trúc | Modular Monolith |
| Multi-tenant | Shared DB + `TenantId` + EF Global Query Filter |
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

- [ ] **Bắt đầu code Phase 0a** theo plan `2026-07-07-phase0a-multitenant-foundation.md`.
  - Cách nhanh: mở Claude Code, nói *"triển khai plan Phase 0a"* và chọn Subagent-Driven hoặc Inline.
  - Hoặc tự làm: theo Task 1→7, mỗi task commit một lần.
- [ ] Sau Phase 0a: cần **plan Phase 0b** (JWT auth, RBAC/permission, đăng ký & provisioning tenant, subscription/plan, khung React + đăng nhập). *Chưa viết — nhờ tạo khi tới đó.*

## Môi trường đã kiểm tra (máy này)
- .NET SDK 10.0.100 ✅ · Node v22.15 ✅ · SQL Server LocalDB (MSSQLLocalDB) ✅
- Cần cài khi bắt đầu Task 7: `dotnet tool install --global dotnet-ef`

## Cấu trúc repo hiện tại
```
TourKit/
├─ script.sql                  # DB hệ cũ (tham chiếu nghiệp vụ, KHÔNG dùng lại schema)
├─ .editorconfig               # ép style/naming (đã có)
├─ Directory.Build.props       # ép nullable + warnings-as-errors + analyzers (đã có)
└─ docs/
   ├─ README.md                # file này
   ├─ conventions/             # best-practice backend + frontend
   └─ superpowers/
      ├─ specs/                # thiết kế
      └─ plans/                # kế hoạch code
```
*Source code (`src/`, `web/`, `tests/`) sẽ được tạo khi chạy Phase 0a.*
