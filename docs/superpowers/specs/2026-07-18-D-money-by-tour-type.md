# Item D — Báo cáo thu chi theo loại tour (MoneyReport nhánh FIT + hoàn/huỷ)

**Ngày:** 2026-07-18 · **Effort:** M · **Trạng thái:** ✅

## Quyết định phạm vi (PM, tự quyết)
Legacy MoneyReport cực phức tạp (cơ chế FIT-sub-booking trong đơn đoàn, receipt_FIT tách khỏi đơn cha, status-code riêng 104…) — **model tourkit-crm không có** cấu trúc đó. Tái tạo nguyên bản = rủi ro bịa. → Làm bản **đúng-chắc theo model hiện tại**: gom theo `Order.BookingType`, trừ `Order.TotalRefund` (hoàn/huỷ) ra doanh thu ròng. Đạt đúng tinh thần "nhánh FIT + hoàn huỷ" mà không chế cơ chế legacy.

## Thiết kế
- Query `GetMoneyByTourTypeAsync()`: gom đơn theo BookingType → `gross=ΣTotalRevenue`, `refund=ΣTotalRefund`, `net=gross-refund`, `cost=ΣOrderCost.ActualAmount`, `profit=net-cost`. Nhãn loại tour từ dict tĩnh (FIT/GIT/Land-Combo/Booking phòng/Dịch vụ lẻ/Visa/Xe).
- DTO `MoneyByTourTypeRowDto` + IReportQueries/IReportService + endpoint `GET /reports/money-by-tour-type` [ReportTurnoverView].
- FE: `MoneyByTourTypeReportPage` (bảng + Excel/CSV, cột hoàn/huỷ đỏ) + route + menu "Thu chi theo loại tour".
- Test: gom 2 loại + trừ hoàn đúng; rỗng khi không đơn. 460 PASS.

## Không làm (YAGNI)
FIT-sub-booking receipt/payment splitting của legacy; lọc khoảng ngày (thêm sau nếu cần, như report khác).
