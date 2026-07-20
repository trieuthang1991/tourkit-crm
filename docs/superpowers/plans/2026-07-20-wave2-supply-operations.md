# Wave 2 — Cung ứng & Điều hành tour (plan chi tiết theo màn)

> Điều kiện: Wave 1 xong (cần đơn/chuyến để gắn dịch vụ). Chuẩn chung như Wave 1 (khung Wave 0 + UI-CONVENTIONS + soi hệ cũ trước khi code + verify Chrome + commit/màn).

## 2.1 Nhà cung cấp (Providers) `/Providers` — M
List + offcanvas (loại NCC, thông tin thanh toán, điều khoản PaymentTerms, thị trường MarketTypes — modal con trong React chuyển thành pills trong trang chi tiết NCC). Trang chi tiết `_TkDetailShell`: thông tin + tab Bảng giá dịch vụ + tab Công nợ (link report). Phục vụ 3 leaf: p-all, f-provider (lọc loại=hàng không), gd-provider.
**Nghiệm thu:** tạo NCC khách sạn + gán điều khoản TT; chi tiết hiện đủ tab.

## 2.2 Danh mục dịch vụ (ServiceItems) `/ServiceItems` — S
CRUD khung chuẩn. **Nghiệm thu:** CRUD + search không dấu.

## 2.3 Bảng giá NCC (ProviderServices) `/ProviderServices` — S+
CRUD + lookup Provider/ServiceItem/Currency; filter theo NCC. Leaf p-pricing.
**Nghiệm thu:** nhập giá phòng theo mùa cho NCC 2.1; Quote calculator (Wave 1) chọn được giá này.

## 2.4 Booking dịch vụ (ServiceBookings) `/ServiceBookings` — M
List + offcanvas booking (NCC, dịch vụ, đơn liên quan, ngày, tiền) + **drawer lịch thanh toán** (ServicePaymentTerms — bảng dòng kỳ hạn, tái dùng pattern bảng dòng của Invoice/Quote).
**Nghiệm thu:** booking phòng gắn đơn Wave 1; kỳ hạn thanh toán sinh nhắc đúng.

## 2.5 Phiếu điều hành dịch vụ (ServiceOperations) `/ServiceOperations` — M
List phiếu điều hành theo chuyến + trạng thái; form gộp dịch vụ theo chuyến; in phiếu qua `_PrintLayout`.
**Nghiệm thu:** tạo phiếu điều hành cho chuyến 1.8, in A4.

## 2.6 Quỹ phòng (RoomFund) `/RoomFund` — **L (allotment grid)**
**Soi kỹ hệ cũ trước** (memory `legacy-nghiepvu-4-module` — allotment). Grid: hàng = hạng phòng×NCC, cột = ngày (tháng), ô = còn/giữ/bán (màu trạng thái), click ô = offcanvas cập nhật. Viết component `tk.allotment` (bảng HTML thuần + fixed cột đầu, KHÔNG lib ngoài) tại đây.
**Nghiệm thu:** nhập allotment 1 KS 30 ngày; booking 2.4 trừ quỹ đúng ô.

## 2.7 Vé máy bay đoàn + lẻ `/FlightTickets`, `/FlightTicketsIndividual` — M×2
2 màn cùng khung: list vé (PNR, chặng, hạn xuất, NCC, tiền) + offcanvas; vé lẻ có P&L theo vé (memory legacy-nghiepvu). Gắn TicketFunds (series vé — CRUD S kèm theo, leaf p-series).
**Nghiệm thu:** nhập series vé → xuất vé lẻ từ series, quỹ giảm; cảnh báo hạn xuất vé.

## 2.8 Hướng dẫn viên `/GuideAssignments` + `/GuideSchedule` — M + **L (Gantt)**
Bảng phân công (list + offcanvas gán HDV vào chuyến + tạm ứng/bàn giao GuideTransactions). **Gantt lịch điều** — viết `tk.gantt` TẠI ĐÂY (thanh ngang theo HDV×ngày, data từ assignments; HTML/CSS thuần hoặc frappe-gantt nếu quyết thêm lib — mặc định thuần cho nhẹ). Trùng lịch phải cảnh báo (P0 trùng lịch đã có backend).
**Nghiệm thu:** gán 1 HDV 2 chuyến chồng ngày → báo trùng; Gantt hiện đúng thanh.

## 2.9 Quản lý xe `/Vehicles` (S) + `/VehicleAssignments` (M) + `/VehicleSchedule` (L)
Kho xe CRUD; điều xe (chờ duyệt/duyệt); lịch điều xe **tái dùng `tk.gantt`** (2.8) đổi nguồn dữ liệu.
**Nghiệm thu:** điều 1 xe cho chuyến, duyệt, Gantt hiện; trùng lịch xe báo lỗi.

## Chốt Wave 2
Demo luồng vận hành trọn: đơn Wave 1 → booking phòng (trừ quỹ) + vé bay + điều HDV + điều xe + phiếu điều hành in được. Test xanh, cập nhật memory.
