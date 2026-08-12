# Kiểm kê màn hình: bản mới so với KojiCRM (hệ cũ)

> Bước 1 của đợt rà soát. Mục tiêu: biết **còn thiếu bao nhiêu**, có bằng chứng, chứ không phải cảm giác.
> Cập nhật 2026-08-12.

## Cách lấy số liệu

| Nguồn | Lấy gì |
|---|---|
| `tourkit/CMS/KojiCRM/Pages/Controls/MenuLeft.ascx` | menu hệ cũ — 19 nhóm |
| `src/TourKit.Api/Pages/Shared/_MenuData.cs` | menu bản mới — 107 mục |
| `src/TourKit.Api/Routing/RouteMap.cs` | 96 route có thật |

Đối chiếu tự động: **mọi mục menu bản mới đều trỏ tới route CÓ THẬT** (0 mục chết). Điều này chỉ chứng
minh trang mở được — KHÔNG chứng minh trang làm đủ việc. Xem mục "Giới hạn" ở cuối.

---

## 1. So theo NHÓM

| Nhóm hệ cũ | Bản mới | Ghi chú |
|---|---|---|
| Workspace | ✅ | thiếu 2 mục con (xem §2) |
| Nhà cung cấp | ✅ | hệ cũ sinh menu con ĐỘNG theo danh mục loại dịch vụ; bản mới cố định 5 mục |
| CRM | ✅ | bản mới có thêm Khách tiềm năng, Rà khách trùng, Báo cáo phễu |
| Báo Giá | ✅ | thêm Báo giá đại lý (B2B) |
| Đơn hàng/LKH | ✅ | |
| Booking Phòng/Khách sạn | ✅ | thêm Hạng phòng |
| Vé Máy Bay | ⚠️ | hệ cũ có **Visa** trong nhóm này; bản mới để Visa ở Báo giá + Đơn hàng |
| Hướng dẫn viên | ✅ | |
| Quản lý xe | ✅ | |
| Điều hành Tour | ✅ | thêm Lịch điều hành |
| Tài chính/Kế toán | ✅ | thêm 2 màn công nợ |
| KPIs | ✅ | |
| Hoa Hồng | ✅ | thêm Chính sách bậc thang, HH theo loại khách |
| Dự án & Công việc | ✅ | |
| **HRM** | ❌ **CHƯA CÓ** | nhóm duy nhất thiếu hẳn |
| Marketing | ✅ | thêm Bài viết, Chuyên mục |
| Báo cáo | ✅ | thêm Thu chi theo loại tour |
| Cài đặt hệ thống | ✅ | thêm Vai trò & quyền, Gói dịch vụ |
| Log hệ thống | ✅ | |
| — | ➕ **Đại lý (B2B)** | nhóm bản mới có, hệ cũ không |

**18/19 nhóm đã có. Thiếu duy nhất HRM.**

---

## 2. Mục CÒN THIẾU (có ở hệ cũ, chưa có ở bản mới)

| Mục | Nhóm | Vì sao chưa làm |
|---|---|---|
| Toàn bộ **HRM** | HRM | cần đặc tả nghiệp vụ — đã ghi ở `HANDOFF.md` mục (C) |
| **Mở HRM** | Workspace | phụ thuộc HRM |
| **Hướng dẫn sử dụng** | Workspace | chưa có nội dung hướng dẫn |
| **Visa** trong nhóm Vé Máy Bay | Vé Máy Bay | chức năng CÓ (ở Báo giá + Đơn hàng), chỉ khác chỗ đặt trên menu |

---

## 3. Mục chỉ là CHỖ GIỮ CHỖ (menu có, bấm vào ra trang "sắp có")

Đây là phần dễ nhầm nhất khi nhìn menu: mục vẫn hiện, vẫn bấm được, nhưng chưa có gì bên trong.

| Mục | Nhóm |
|---|---|
| Mạng Nội Bộ | Workspace |
| Feedback ZNS | CRM |
| Thông tin OA | Marketing |
| ZNS | Marketing |
| Zalo UID (Tin follow OA) | Marketing |
| Xuất báo cáo | Báo cáo |

**6 mục.** Bốn trong số đó (Feedback ZNS, Thông tin OA, ZNS, Zalo UID) đều chờ **khoá API của nhà cung
cấp Zalo** — cùng một nút thắt, mở được là làm được cả bốn. Xem `external-providers-gateway`.

---

## 4. Phần bản mới CÓ THÊM so với hệ cũ

Không phải để khoe — để biết những màn này **không có bản cũ mà đối chiếu**, nên chỉ đánh giá được
theo nghiệp vụ chứ không theo "giống hệ cũ chưa".

Đại lý (B2B) · Khách tiềm năng · Rà khách trùng · Báo cáo phễu cơ hội · Lịch điều hành ·
Công nợ khách · Công nợ NCC · Hạng phòng · Vai trò & quyền · Gói dịch vụ · Bài viết ·
Chuyên mục bài viết · Thu chi theo loại tour · Chính sách hoa hồng bậc thang · HH theo loại khách.

---

## 5. GIỚI HẠN của bản kiểm kê này

Bản này trả lời **"màn đó có tồn tại không"**, KHÔNG trả lời **"màn đó làm đủ việc chưa"**.

Ba thứ nó không thấy:

1. **Chức năng trong màn.** Một màn danh sách có thể mở được nhưng thiếu bộ lọc, thiếu xuất file,
   thiếu thao tác hàng loạt so với hệ cũ. Đối chiếu được thì phải mở từng màn hệ cũ mà so.
2. **Nghiệp vụ ẩn sau nút bấm.** Ví dụ vừa phát hiện trong đợt này: "Cơ hội bán hàng" của bản mới
   trước đây trỏ vào thực thể `Lead` — menu trông đủ, nhưng khái niệm sai hẳn.
3. **Dữ liệu.** Hệ cũ ~127 bảng; bản mới ít hơn. Bảng nào chưa có thì màn tương ứng có mở được cũng
   thiếu cột.

→ Đó là nội dung của **bước 2** (soi CSDL) và **bước 3** (soát chức năng từng màn).

---

## 6. Việc tiếp theo, xếp theo mức đau

| # | Việc | Vì sao |
|---|---|---|
| 1 | Soát chức năng **từng màn** trong 5 nhóm dùng nhiều nhất (CRM, Đơn hàng, Tài chính, Báo giá, Điều hành) | đây là chỗ người dùng ở suốt ngày |
| 2 | Soi CSDL: chỉ mục, chỗ nạp cả bảng rồi lọc trong bộ nhớ | ảnh hưởng mọi màn |
| 3 | Lấy khoá API Zalo → mở được 4 mục giữ chỗ cùng lúc | một nút thắt, bốn kết quả |
| 4 | Đặc tả HRM | nhóm duy nhất thiếu hẳn, nhưng ít người dùng hằng ngày |
| 5 | Thêm Visa vào menu nhóm Vé Máy Bay | công sức gần bằng không |
