---
name: screen-needs-search-and-stats
description: Mỗi màn danh sách phải có thanh search/lọc + hàng thẻ thống kê phía trên (bám hệ cũ), không chỉ bảng
metadata:
  type: feedback
  originSessionId: 4a96fa90-2f6b-4adb-9b7f-e76d2a5b864b
---

Chủ dự án (2026-07-12): các màn hệ mới ĐANG CHỈ CÓ MỖI DANH SÁCH. Bám hệ cũ (staging.tourkit.vn),
mỗi màn danh sách cần THÊM:
1. **Hàng thẻ THỐNG KÊ** phía trên (vd Data khách hàng: TỔNG SỐ KHÁCH HÀNG / KHÁCH TẠO MỖI NGÀY /
   MỖI THÁNG / MUA LẦN ĐẦU / MUA LẠI NHIỀU LẦN).
2. **Thanh SEARCH + BỘ LỌC** (vd: ô tìm kiếm nhanh, Chọn chi nhánh, Chọn nhóm, phễu khách hàng, cỡ trang,
   Xuất/Nhập file). Áp dụng cho MỌI màn danh sách phù hợp.

**How to apply:** Backend thêm endpoint stats + tham số search/filter cho list. FE: hàng thẻ stat + toolbar
lọc trên bảng (mở rộng ResourcePage hoặc build màn custom). Trước khi làm 1 màn, XEM staging màn đó để lấy
đúng bộ stat + filter. Bắt đầu từ Data khách hàng làm mẫu chuẩn rồi roll-out.

Xem [[legacy-ui-reference]], [[entity-extend-json-string-pattern]], [[screen-needs-search-and-stats]].
