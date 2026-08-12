# Soát thiết kế CSDL & truy vấn

> Bước 2 của đợt rà soát. Câu hỏi: **truy vấn có nhanh không, và thiết kế có dễ dùng không.**
> Mọi con số dưới đây đo bằng cách quét mã, không phải ước lượng. Cập nhật 2026-08-12.

## Tóm tắt

| Hạng mục | Tình trạng |
|---|---|
| Chỉ mục (index) | ✅ **82/83** cấu hình entity có index — không phải chỗ đau |
| Phân trang | ❌ **23/35** dịch vụ danh sách nạp cả bảng rồi cắt trang trong bộ nhớ |
| Đường nhanh SQL | ❌ **16/23** trong số đó **không có đường nhanh nào** |
| Cột chuỗi chứa danh sách | ⚠️ còn ở jsonb "trường mềm" — cố ý, nhưng có giá |
| Tra danh bạ nhân viên | ⚠️ **8 dịch vụ** nạp cả bảng người dùng mỗi lần gọi |

**Kết luận: chỉ mục không phải vấn đề. Vấn đề là chỗ THỰC HIỆN phép lọc — ở bộ nhớ thay vì ở SQL.**

---

## 1. Phân trang trong bộ nhớ — vấn đề lớn nhất

### Hiện trạng

23 dịch vụ theo khuôn: `repo.ListAsync(vị-từ)` → **materialize toàn bộ kết quả** → `.Where()` tiếp →
`.OrderBy()` → `.Skip((page-1)*size).Take(size)`.

Nghĩa là mở trang 1 của màn Đơn hàng cũng kéo **mọi đơn khớp bộ lọc** về bộ nhớ ứng dụng, rồi vứt đi
tất cả trừ 20 dòng.

Trong 23 dịch vụ đó, **16 dịch vụ không có đường nhanh nào**:

`AgentBookingService` · `AgentQuoteRequestService` · `BookingService` · `CampaignService` ·
`CommissionRuleService` · `CustomerCareService` · `CustomerCommissionRuleService` ·
`FlightTicketIndividualService` · `FlightTicketService` · `GuideAssignmentService` ·
`LeadCampaignService` · `PostService` · `ServiceOperationService` · `TicketFundService` ·
`VehicleAssignmentService` · `WorkTaskService`

### Vì sao chưa ai kêu

Dữ liệu demo còn nhỏ. Cách hỏng của lỗi này là **chậm dần đều**, không phải đổ vỡ: tháng đầu 200 đơn
thì không ai thấy gì, năm sau 50.000 đơn thì mỗi lần mở màn kéo về đủ 50.000 dòng. Không có thông báo
lỗi nào, chỉ là trang lâu dần — và tới lúc đó rất khó nối ngược về nguyên nhân.

### Khuôn ĐÚNG đã có sẵn trong kho

`CustomerService.ListAsync` — đọc file này trước khi sửa bất kỳ dịch vụ nào khác:

- **Đường nhanh**: không có bộ lọc jsonb → `repo.PageAsync(page, size, vị-từ)`, đếm và cắt trang ở SQL,
  chỉ gộp số liệu phụ cho ĐÚNG 20 dòng của trang.
- **Đường chậm**: chỉ khi bộ lọc chạm vào trường jsonb (tỉnh, giới tính, chi nhánh, thẻ…) — thứ SQL
  không dịch được.

Chia hai đường như vậy giữ được sự tiện lợi của jsonb mà không bắt mọi truy vấn trả giá cho nó.

### Đề xuất, xếp theo mức đau

| # | Dịch vụ | Vì sao trước |
|---|---|---|
| 1 | `BookingService.ListOrdersAsync` | bảng Orders lớn nhanh nhất, màn dùng nhiều nhất |
| 2 | `ReceiptService`, `PaymentService` | phiếu thu/chi sinh mỗi ngày, không bao giờ giảm |
| 3 | `DepartureService.ListAsync` | chuyến chỉ tăng; đã gây một lỗi thật trong đợt này (xem §4) |
| 4 | `WorkTaskService`, `CustomerCareService` | mỗi nhân viên vài việc/ngày × cả công ty |
| 5 | phần còn lại | danh mục có biên, ít đau hơn |

---

## 2. Tra danh bạ nhân viên mỗi lần gọi

8 dịch vụ gọi `userRepo.ListAsync()` — nạp **toàn bộ** bảng người dùng — chỉ để đổi id thành tên hiển thị:

`UserAdminService` · `BookingService` · `CommissionCampaignService` · `LeadCampaignService` ·
`CustomerService` · `ApprovalProcessService` · `WorkflowService` · `WorkTaskService`

Kho **đã có** `TourKit.Api.Services.UserDirectory` (cache 60 giây, tách theo tenant) và `CLAUDE.md`
đã ghi luật dùng nó. Nhưng nó nằm ở tầng Api còn các dịch vụ này ở tầng Application, nên không với tới.

**Cách sửa đúng hướng phụ thuộc:** đưa một `IUserDirectory` xuống Application (interface ở Application,
hiện thực có cache ở Api/Infrastructure). Không nên bê `UserDirectory` ngược lên — sẽ phá chiều phụ
thuộc mà `ArchTests` đang canh.

---

## 3. jsonb "trường mềm" — tiện, nhưng phải biết giá

Mẫu `CustomerCrmProfile` / `ProviderProfile` / `ProviderServiceLineProfile` gom các trường mềm vào một
cột jsonb. Đổi lại được: thêm trường không cần migration.

Giá phải trả: **mọi bộ lọc chạm vào trường jsonb đều rơi xuống đường chậm** — nạp cả bảng rồi lọc ở bộ
nhớ. Ở màn Khách hàng, 19 tiêu chí lọc nằm trong nhóm này.

Không đề xuất bỏ jsonb. Hai hướng đỡ hơn, theo thứ tự nên thử:

1. **Nâng những trường LỌC NHIỀU NHẤT thành cột thật** (tỉnh/thành, chi nhánh, người phụ trách). Vẫn
   giữ jsonb cho phần đuôi dài. Đây là cách rẻ nhất và bỏ được phần lớn đường chậm.
2. **Index GIN cho jsonb** nếu vẫn cần lọc trong đó — Postgres làm được, EF gọi được qua
   `EF.Functions`. Nhưng phải thêm EF vào tầng Application, nên cân nhắc sau hướng 1.

---

## 4. Bài học đã trả giá trong đợt này

Khi làm ô chọn chuyến gọi server, bản đầu tôi định dùng lại `DepartureService.ListAsync`. Đọc kỹ mới
thấy nó nạp cả bảng chuyến rồi lọc trong bộ nhớ, và với mỗi chuyến ở trang hiện tại còn nạp thêm đơn
hàng cùng chỗ ngồi để tính Giữ/Bán/Còn.

Dùng nó cho ô gợi ý nghĩa là **mỗi lần người dùng gõ một ký tự lại quét cả bảng** — đắt hơn chính cách
nạp sẵn mà ô đó đang thay thế. Phải viết `LookupAsync` riêng, đẩy lọc/sắp/cắt xuống SQL.

Bài học chung: **hàm `ListAsync` của một dịch vụ không mặc nhiên dùng lại được cho mục đích khác.** Nó
được viết cho màn danh sách, mang theo cả phần làm giàu dữ liệu của màn đó.

---

## 5. Chỗ KHÔNG cần sửa

Nói ra để khỏi ai đi sửa nhầm:

- **Chỉ mục**: 82/83 cấu hình đã có index theo `(TenantId, …)` đúng cột hay lọc. Không phải chỗ đau.
- **Bọc `IRepository<T>`**: skill khuyên bỏ, nhưng 730 bài kiểm thử đang dựa vào `FakeRepository`.
  Gỡ lớp này là việc riêng, không gộp vào đợt tối ưu truy vấn.
- **Danh mục nhỏ** (loại xe, hạng phòng, lý do…): nạp cả bảng là ĐÚNG, chúng có biên.

---

## 6. Thứ tự làm

| # | Việc | Đo được bằng |
|---|---|---|
| 1 | `BookingService.ListOrdersAsync` sang đường nhanh + đường chậm | thời gian mở màn Đơn hàng với 50k đơn |
| 2 | `IUserDirectory` xuống tầng Application, 8 dịch vụ dùng chung | số câu truy vấn mỗi lần mở màn |
| 3 | Nâng 3 trường jsonb lọc nhiều nhất thành cột thật | số màn còn phải đi đường chậm |
| 4 | Bốn dịch vụ còn lại theo thứ tự ở §1 | |

Trước khi sửa từng cái, **thêm một bài kiểm thử đếm số dòng nạp về** — nếu không thì sửa xong không ai
chứng minh được là đã nhanh hơn, và lần refactor sau rất dễ đưa nó về như cũ.
