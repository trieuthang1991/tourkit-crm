# Dự trù giá tour trong Báo giá (Quote Cost Estimation)

**Ngày:** 2026-07-11 · **Trạng thái:** đã chốt với chủ dự án (hướng A; nghiệp vụ chi tiết uỷ quyền chuyên gia)
**Nguồn nghiệp vụ:** legacy KojiCRM `DuTruTours`/`BaoGia` (`percent_loi_nhuan_khach`, `percent_price_tre_em`, `percent_price_tre_nho`, `GIA_BAN`, `loi_nhuan_du_kien`) + chuẩn vận hành tour operator VN.

## 1. Mục tiêu

Nâng `Quote` hiện có (báo giá gõ tay) thành **dự trù giá vốn → giá bán**: nhân viên nhập giá vốn từng dịch vụ (chọn từ bảng giá NCC hoặc gõ), đặt %lợi nhuận từng dòng, hệ tính giá bán, giá theo hạng khách (NL/trẻ em/trẻ nhỏ), tổng vốn/bán/lãi. Đây là phần lõi định giá của cụm BaoGia/DuTru; mẫu in, BillPaymentRequest, sinh Order làm đợt sau.

**Quyết định đã chốt với chủ dự án:**
1. Ưu tiên lõi dự trù giá vốn → giá bán (markup) trước.
2. Markup = **% trên từng dòng** (legacy `percent_loi_nhuan_khach`).
3. **3 hạng khách** NL / trẻ em / trẻ nhỏ; giá trẻ = % giá NL (legacy `percent_price_tre_em/tre_nho`).
4. Giá vốn **chọn từ bảng giá NCC (`ProviderService`), sửa tay được**.
5. Cấu trúc: **hướng A — mở rộng `Quote`/`QuoteLine`**, không tạo aggregate mới.

## 2. Nghiệp vụ chuẩn (chốt bởi chuyên gia, bám legacy)

### 2.1 Phân loại dòng chi phí — chuẩn ngành tour
Mỗi dòng dự trù thuộc 1 trong 2 phạm vi (scope):
- **PerGroup (cả đoàn)** — chi phí phát sinh 1 lần cho cả đoàn, không phụ thuộc số khách: thuê xe, HDV, phà/cầu đường… `Quantity` = số đơn vị cho cả đoàn (vd 4 ngày xe).
- **PerPerson (theo khách)** — chi phí tính trên đầu khách: phòng, bữa ăn, vé tham quan, vé bay, visa, bảo hiểm… `Quantity` = số đơn vị cho **1 người lớn** (vd 3 đêm phòng).

Loại dịch vụ (`ServiceType`): Other=0, Hotel=1, Transport=2, Guide=3, Meal=4, Ticket=5, Visa=6, Flight=7, Insurance=8 — khớp nhóm module hệ cũ (BookingHotel/CarManagement/Visa/AirPlaneTicket/vé).

### 2.2 Công thức giá — một chỗ duy nhất (`QuoteMath`, mirror `OrderMath`)
Ký hiệu: dòng i có `qty`, `unitCost` (giá vốn đơn vị), `marginPercent` (%LN), `unitPrice` (giá bán đơn vị).

```
unitPrice(i)   = unitCost(i) × (1 + marginPercent(i)/100)    # khi unitCost > 0; unitCost = 0 → giữ unitPrice nhập tay (báo giá nhanh cũ)
paxEq          = Adults + Children×ChildPercent/100 + Infants×InfantPercent/100   # số khách quy đổi
perPaxCost     = Σ PerPerson (qty × unitCost)      ;  perPaxSell  = Σ PerPerson (qty × unitPrice)
groupCost      = Σ PerGroup  (qty × unitCost)      ;  groupSell   = Σ PerGroup  (qty × unitPrice)
TotalCost      = perPaxCost × paxEq + groupCost
TotalAmount    = perPaxSell × paxEq + groupSell
TotalProfit    = TotalAmount − TotalCost                      # legacy loi_nhuan_du_kien
AdultPrice     = perPaxSell + (paxEq > 0 ? groupSell/paxEq : 0)   # legacy GIA_BAN
ChildPrice     = AdultPrice × ChildPercent/100
InfantPrice    = AdultPrice × InfantPercent/100
```

Chuẩn nhất quán: chi phí đoàn chia theo **khách quy đổi** (paxEq) — cùng hệ số với giá hạng khách, nên `TotalAmount = AdultPrice×Adults + ChildPrice×Children + InfantPrice×Infants` khớp tuyệt đối, kiểm toán được. Không làm tròn ở tầng tính (lưu decimal 18,2); trình bày do UI. Mặc định %: trẻ em **75**, trẻ nhỏ **50** (chuẩn thị trường VN; sửa được từng báo giá).

**Backward-compat:** báo giá cũ → `Adults/Children/Infants = 0` (paxEq=0), dòng cũ scope PerGroup=0, unitCost=0 → `TotalAmount = Σ qty×unitPrice` y hệt cũ. Không đổi nghĩa dữ liệu.

### 2.3 Nguồn giá vốn
Dòng có thể tham chiếu `ProviderServiceId` (bảng giá NCC) — UI tự điền `unitCost = ContractPrice`, sửa được. Validate: dòng giá tồn tại (theo tenant). Không bắt buộc (dịch vụ ngoài bảng giá gõ tay).

## 3. Thay đổi kỹ thuật

### Entities (additive)
- `Quote` += `Adults`, `Children`, `Infants` (int, def 0); `ChildPercent` (decimal, def 75), `InfantPercent` (def 50); `TotalCost`, `TotalProfit` (decimal, tính khi ghi).
- `QuoteLine` += `ServiceType` (int, def 0), `Scope` (int: PerGroup=0, PerPerson=1), `ProviderServiceId` (Guid?), `UnitCost` (decimal, def 0), `MarginPercent` (decimal, def 0). `UnitPrice` giữ nguyên nghĩa **giá bán đơn vị**.
- `QuoteMath` (Shared/Domain): toàn bộ công thức §2.2 + record kết quả `QuotePricing`.

### Application
- DTO mở rộng tương ứng; `QuoteDto` trả thêm `TotalCost/TotalProfit/AdultPrice/ChildPrice/InfantPrice` (3 giá hạng = derived, không lưu).
- `QuoteService`: mỗi lần ghi — validate `ProviderServiceId` (nếu có), tính `unitPrice` từ cost×(1+margin) khi `unitCost>0`, gọi `QuoteMath` ghi tổng.
- Validator: số khách ≥ 0; `ChildPercent/InfantPercent` 0–100; `MarginPercent` 0–500; `UnitCost ≥ 0`.

### DB
- Migration additive `AddQuoteCostEstimation` (default khớp backward-compat §2.2). Index hiện có giữ nguyên.

### Frontend (`web/src/features/quotes`)
- Header form: 3 ô số khách + 2 ô % hạng.
- `QuoteLinesField`: thêm cột Loại dịch vụ, Phạm vi (đoàn/khách), chọn bảng giá NCC (tự điền giá vốn), Giá vốn, %LN, Giá bán (tự tính, sửa được khi giá vốn=0).
- Panel kết quả: Giá NL / trẻ em / trẻ nhỏ + Tổng vốn / Tổng bán / Lãi dự kiến.
- Quyền: dùng `quote.*` sẵn có (giá vốn/margin chỉ hiện trong CMS staff; B2B agent portal không đổi).

## 4. Error handling
- `ProviderServiceId` không tồn tại → `ValidationAppException` (VN message).
- paxEq = 0 + có dòng PerPerson → hợp lệ (dòng PerPerson không đóng góp tổng — hiển thị cảnh báo UI "chưa nhập số khách").
- Ràng buộc số học qua FluentValidation, không throw từ QuoteMath (pure function).

## 5. Testing
- **Unit `QuoteMathTests`**: chỉ-đoàn (backward-compat), chỉ-theo-khách, hỗn hợp, paxEq lẻ (trẻ em %), paxEq=0, profit, giá 3 hạng khớp TotalAmount.
- **Unit `QuoteServiceTests`** (mở rộng): unitPrice tính từ cost+margin; giữ unitPrice khi cost=0; validate ProviderServiceId; tổng ghi đúng.
- **Web**: schema test cho field mới; typecheck/lint.

## 6. Ngoài phạm vi (đợt sau)
Mẫu in GroupTour/SingleTour · BillPaymentRequest · sinh Order/ServiceBooking từ quote chấp nhận · nhiều bản dự trù cho 1 tour.
