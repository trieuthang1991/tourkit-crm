# Item C — Notification typed/actionable

**Ngày:** 2026-07-18 · **Effort:** M · **Trạng thái:** ✅ (C1) · C2 hoãn (có kiểm soát)

## Bối cảnh
Notification đã có `LinkUrl` (actionable) nhưng **không typed** → feed phẳng, không phân loại/icon. Gap: "1 feed phẳng, không typed/actionable".

## C1 — Typed + actionable (đã làm)
- `Notification.Type` (string, mặc định "system") + migration `AddNotificationType`.
- `INotificationService.PushAsync(..., string type = "system")` + persist + `NotificationDto.Type`.
- Gán type tại emit: giao việc → "task"; duyệt phiếu thu/chi → "approval". (CampaignService không dùng PushAsync — bỏ qua.)
- FE: schema `type`, NotificationsPage render icon + nhãn theo type (approval/task/marketing/system) + tag phân loại.
- Test: mặc định "system"; PushAsync lưu type đã truyền.

## C2 — Notify người khởi tạo (HOÃN, có kiểm soát)
Khi duyệt xong/bị từ chối → báo người tạo phiếu (đóng nợ P0-5). Cần `StartedByUserId` trên ReceiptApproval/PaymentApproval (proxy cho "creator", tránh đụng voucher creation) + wire vào state machine terminal (`Approved`/`RejectedTerminal`). **Hoãn** vì đụng máy trạng thái duyệt đa cấp (54 test nhạy cảm) — làm thành slice riêng, không rush trong đợt chạy tự động. Ghi để PM quyết.
