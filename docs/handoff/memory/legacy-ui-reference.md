---
name: legacy-ui-reference
description: Hệ cũ TourKit live để bám GIAO DIỆN (staging URL + brand tokens + layout trang chủ)
metadata:
  type: reference
  originSessionId: 4a96fa90-2f6b-4adb-9b7f-e76d2a5b864b
---

Chủ dự án yêu cầu FE mới **bám đúng giao diện hệ cũ** (2026-07-12). Nguồn tham chiếu SỐNG:
- **URL:** https://staging.tourkit.vn/ — tài khoản `admin` (mật khẩu do user tự nhập; Claude KHÔNG được gõ mật khẩu).
- Đăng nhập bằng cách user nhập password vào Browser pane; Claude tiếp quản duyệt.

**Brand tokens (đo từ staging, đã áp vào providers.tsx commit 41776a2):**
- colorPrimary = **#EB5324** (đỏ-cam TourKit). Badge đỏ tươi #FD2A00, gradient cam #F75C14.
- Sidebar nền **#333333** (charcoal), mục active nền đỏ #EB5324, chữ trắng mờ.
- Font **Roboto**. borderRadius 8.

**Menu hệ cũ (top-level, khớp MenuLeft.ascx):** Workspace (Mạng Nội Bộ/Bàn làm việc/Tổng quan/Thông báo/HDSD) ·
Nhà cung cấp · CRM · Báo Giá · Đơn hàng/LKH · Booking Phòng/Khách sạn · Vé Máy Bay · Hướng dẫn viên ·
Quản lý xe · Điều hành Tour · Tài chính/Kế toán · KPIs · Hoa Hồng · Dự án & Công việc · HRM · Marketing · Báo cáo · Cài đặt.
(Bản mới gộp Booking/Vé/Visa vào "Sản phẩm Tour", gộp Xe+HDV — có thể tách lại nếu cần bám sát.)

**Trang chủ hệ cũ = "Bàn làm việc"** (KHÁC dashboard số liệu bản mới): hồ sơ chuyên viên (avatar, chức danh,
ngày vào làm, thâm niên) + donut **Tỉ lệ công việc** (chưa bắt đầu/đang thực hiện/kiểm tra/hoàn thành/huỷ) +
thanh nút **Tạo việc/Cơ hội/Lịch hẹn/Data khách/Đơn** + feed **Thông báo** (tab Tất cả/Đơn/Duyệt/Lịch) +
widget **OUTSTANDING – Công nợ khách hàng** (xếp hạng #1..N khách + số nợ) + **Lịch hẹn hôm nay**.

Xem [[business-logic-follow-old-project]], [[roadmap-status]].
