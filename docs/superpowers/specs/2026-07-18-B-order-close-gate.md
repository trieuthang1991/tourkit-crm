# Item B — Cổng chốt đơn (Order close gate)

**Ngày:** 2026-07-18 · **Effort:** M · **Trạng thái:** ⏳
**Phạm vi (user chốt):** "Cổng chốt đơn" — KHÔNG làm workflow duyệt đơn đa cấp (để riêng, tránh chồng Item C).

## Bối cảnh
Đơn hiện: `Draft(1)→Confirmed(2)→Cancelled(3)`, không có "chốt đơn" cấp đơn. Legacy có khái niệm ChotDon; luồng gốc: duyệt booking → xuất HĐ → hoa hồng → chốt. Order đã có `IsPaymentRecognized` (legacy IsGhiNhanDongTien) + `IsCommissionSettled`. Không có flag hoá đơn → bỏ gate HĐ (YAGNI).

## Thiết kế
- **Enum** `OrderStatus.Closed = 4`.
- **Entity** Order thêm `DateTimeOffset? ClosedAt`, `Guid? ClosedByUserId` (audit) → migration `AddOrderClose`.
- **CloseOrderAsync(orderId, userId)** — gate BẮT BUỘC tuần tự (mỗi vi phạm ném `ValidationAppException` tiếng Việt):
  1. `Status == Confirmed` (chỉ chốt đơn đã xác nhận; Draft/Cancelled/Closed → chặn).
  2. `IsPaymentRecognized == true` (chưa ghi nhận đủ dòng tiền → chặn).
  3. `IsCommissionSettled == true` (hoa hồng chưa quyết → chặn).
  → set `Status=Closed, ClosedAt=now, ClosedByUserId=userId`.
- **ReopenOrderAsync(orderId)** — `Status==Closed` → `Confirmed`, xoá ClosedAt/ClosedByUserId (sửa sai). Cùng permission (booking.create).
- **Khoá sửa khi Closed:** `AssignSalesAsync` chặn nếu đơn Closed (`EnsureNotClosed`). Các mutation cấp chỗ (deposit/cancel qua TourCustomer) — ghi chú follow-up, chưa khoá đợt này.
- **API:** `POST /orders/{id}/close` + `POST /orders/{id}/reopen`, `[Authorize(BookingCreate)]`, acting user từ `ICurrentUser` (401 nếu thiếu). Tái dùng BookingCreate (tránh seed permission mới + backfill Admin — như P0-6); tách `order.close/reopen` sau nếu cần.
- **FE:** OrdersPage thêm nút "Chốt đơn"/"Mở lại" theo trạng thái + nhãn/màu cho Closed.

## Test (TDD)
1. Đủ điều kiện → chốt: Status=Closed, ClosedAt set.
2. Chưa Confirmed / chưa PaymentRecognized / chưa CommissionSettled → ValidationAppException (3 ca).
3. Reopen: Closed→Confirmed, ClosedAt=null.
4. AssignSales trên đơn Closed → chặn.

## Không làm (YAGNI)
Workflow duyệt đơn đa cấp; gate hoá đơn; khoá toàn bộ mutation cấp chỗ; permission order.* riêng.
