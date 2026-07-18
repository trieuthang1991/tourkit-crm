# P1 — Kế hoạch triển khai & Quy trình làm việc (PM)

**Ngày:** 2026-07-18
**Phạm vi "Xong":** hết P1 (P2 tích hợp ngoài để lịch riêng vì cần credential).
**Chuẩn nghiệp vụ:** legacy `tourkit` tại `D:\MiGroup\tourkitapp\tourkit` (index GitNexus `tourkit`). Không bịa logic — đối chiếu legacy.

---

## A. Quy trình cho MỖI hạng mục (gate chống sai)

1. **Spec ngắn** → `docs/superpowers/specs/YYYY-MM-DD-<topic>.md`, đối chiếu legacy.
2. **Impact** → `gitnexus_impact` trên symbol sẽ sửa; cảnh báo user nếu HIGH/CRITICAL.
3. **TDD** → viết test trước, rồi implement.
4. **Verify gate** → kill :5075 → `dotnet build` + toàn bộ test PASS + `tsc` + `vite build` (nếu chạm FE).
5. **detect_changes** → `gitnexus_detect_changes` xác nhận scope.
6. **Commit** → 1 commit/hạng mục (tách BE/FE nếu lớn), message tiếng Việt theo convention repo + `Co-Authored-By: Claude Opus 4.8`.
7. **Cập nhật docs** → tick ✅ ở §5 `PM-nghiepvu-gap-analysis.md` + memory nếu có quyết định nghiệp vụ.
8. **Re-index** → `npx gitnexus analyze` sau commit.

**Bất biến:** không commit khi test đỏ; mỗi commit tự đứng được; báo cáo user sau mỗi hạng mục.

---

## B. Thứ tự triển khai P1

| # | Hạng mục | Trạng thái | Effort |
|---|----------|-----------|--------|
| A | Hoàn tất Hoa hồng bậc thang (finish UI + wire tier vào order-commission/report) | ✅ | S-M |
| B | Cổng chốt/tất toán đơn (OrderStatus.Closed + gate + khoá sửa) | ✅ | M |
| C | Notification typed/actionable + notify creator (thêm `CreatedByUserId`) | ⬜ | M |
| D | MoneyReport nhánh FIT + hoàn/huỷ chỗ | ⬜ | M |
| E | Customer dedup + auto-chia lead + import | ⬜ | M-L |
| F | Excel export server-side (all-pages) | ⬜ | S-M |
| G | Đối trừ/đối soát công nợ NCC (cần schema mới) | ⬜ | M |

Ký hiệu: ⬜ chưa làm · ⏳ đang làm · ✅ xong (build sạch, test pass, commit).

---

## C. Docs duy trì
- `docs/superpowers/specs/*` — spec ngắn mỗi hạng mục.
- `docs/PM-nghiepvu-gap-analysis.md` §5 — sổ tiến độ P1.
- Memory — quyết định nghiệp vụ quan trọng.
