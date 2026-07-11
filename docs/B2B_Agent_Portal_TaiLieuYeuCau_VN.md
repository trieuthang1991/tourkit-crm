# TÀI LIỆU YÊU CẦU — B2B AGENT PORTAL
### Phạm vi theo giai đoạn: MVP, Tự động hóa nghiệp vụ & Tối ưu hóa nghiệp vụ

| Trường | Nội dung |
|---|---|
| **Tên tài liệu** | B2B Agent Portal — Tài liệu Yêu cầu (Phạm vi theo giai đoạn) |
| **Khách hàng (Client)** | *(để trống)* |
| **Đơn vị lập** | Kaopiz Global JSC |
| **Trạng thái** | Bản nháp (Draft) — phục vụ rà soát nội bộ |
| **Phiên bản** | v1.0 |
| **Ngày** | 07/07/2026 |
| **Nguồn** | Tổng hợp lại từ tài liệu yêu cầu do khách hàng cung cấp (quyền truy cập chỉ đọc) |

> **Lưu ý bản chất tài liệu:** Đây là **tài liệu yêu cầu / phạm vi (requirements & scope)**, mô tả *cần làm gì* theo từng giai đoạn. Đây **chưa phải** bản thiết kế kỹ thuật (kiến trúc, mô hình dữ liệu, thiết kế API) — phần đó sẽ được xây dựng ở bước Thiết kế sau.

> **Quy ước thuật ngữ:** **Đại lý (Agent)** = đại lý du lịch B2B (là doanh nghiệp/nhân sự, không phải phần mềm tự động). Trong tài liệu, lần đầu ghi "Đại lý (Agent)", các lần sau gọi tắt là **"Đại lý"**. Riêng tên riêng/tên module giữ nguyên tiếng Anh (B2B Agent Portal, Agent Profile, Agent-specific Pricing…).

> ℹ️ **Quy ước diễn giải:** Các đoạn được đánh dấu **_› Diễn giải:_** (chữ nghiêng) là **phần giải thích bổ sung để dễ hiểu**, do đơn vị lập tài liệu diễn giải theo thông lệ ngành — **KHÔNG thuộc nội dung yêu cầu gốc** của khách hàng. Khi có mâu thuẫn, **nội dung yêu cầu gốc là chuẩn**; phần diễn giải cần được khách hàng xác nhận lại.

---

## Mục lục

1. [Giới thiệu](#1-giới-thiệu)
2. [Bối cảnh khách hàng](#2-bối-cảnh-khách-hàng)
3. [Chiến lược phát triển](#3-chiến-lược-phát-triển)
4. [Giai đoạn 1 — MVP (Sản phẩm khả dụng tối thiểu)](#4-giai-đoạn-1--mvp-sản-phẩm-khả-dụng-tối-thiểu)
5. [Giai đoạn 2 — Tự động hóa nghiệp vụ (Business Automation)](#5-giai-đoạn-2--tự-động-hóa-nghiệp-vụ-business-automation)
6. [Giai đoạn 3 — Tối ưu hóa nghiệp vụ (Business Optimization)](#6-giai-đoạn-3--tối-ưu-hóa-nghiệp-vụ-business-optimization)
7. [Lưu ý & Yêu cầu bổ sung cho Agent Portal](#7-lưu-ý--yêu-cầu-bổ-sung-cho-agent-portal)

---

## 1. Giới thiệu

Tài liệu này mô tả yêu cầu và phạm vi triển khai cho dự án **B2B Agent Portal**, được xây dựng dựa trên tài liệu yêu cầu (requirement) do phía khách hàng cung cấp. Tài liệu trình bày định hướng phát triển theo từng giai đoạn (Phase-based Development) cùng phạm vi chi tiết của từng giai đoạn.

> **Lưu ý:** Nội dung yêu cầu nghiệp vụ trong tài liệu này được tổng hợp và trình bày lại **nguyên trạng** từ tài liệu gốc của khách hàng, **không bổ sung hay suy diễn** thêm thông tin ngoài phạm vi đã được cung cấp.

## 2. Bối cảnh khách hàng

Khách hàng đóng vai trò là **DMC** (Destination Management Company — công ty quản lý điểm đến), cung cấp sản phẩm/dịch vụ du lịch cho hệ thống **Đại lý du lịch (Travel Agent)** B2B — là các doanh nghiệp và nhân sự, không phải phần mềm tự động.

Khách hàng mong muốn xây dựng **B2B Agent Portal** nhằm **số hóa quy trình giao dịch** giữa DMC và các Đại lý (Agent), thay thế cho quy trình thủ công qua **Email/Excel** hiện tại.

## 3. Chiến lược phát triển

Để đảm bảo dự án triển khai nhanh nhưng vẫn đáp ứng mục tiêu phát triển lâu dài, B2B Agent Portal sẽ được phát triển theo **từng giai đoạn (Phase-based Development)**.

- Mỗi giai đoạn tập trung giải quyết một nhóm bài toán nghiệp vụ cụ thể.
- Chức năng của giai đoạn sau được xây dựng dựa trên **nền tảng kiến trúc của giai đoạn trước**, tránh phải thay đổi cơ sở dữ liệu, API hoặc luồng nghiệp vụ khi mở rộng.
- **MVP không phải là phiên bản rút gọn** của sản phẩm, mà là **phiên bản đầu tiên của hệ thống với đầy đủ nền tảng kỹ thuật để mở rộng** trong tương lai.

---

## 4. Giai đoạn 1 — MVP (Sản phẩm khả dụng tối thiểu)

### 4.1 Mục tiêu

Xây dựng nền tảng giao dịch đầu tiên giữa Đại lý và khách hàng DMC.

Mục tiêu của MVP là **thay thế quy trình xử lý Báo giá (Quote) và Đặt chỗ (Booking)** hiện đang thực hiện qua Email và Excel bằng một Portal thống nhất, đồng thời **đồng bộ dữ liệu với CRM**.

MVP tập trung vào việc **chuẩn hóa quy trình nghiệp vụ**, thay vì tự động hóa toàn bộ hệ thống.

### 4.2 Phạm vi thực hiện

#### 4.2.1 Xác thực (Authentication)

Cho phép Đại lý đăng nhập vào hệ thống bằng tài khoản được cấp.

> _› Diễn giải:_ Tài khoản do phía DMC cấp (Đại lý không tự đăng ký ở MVP). Đây là cổng vào để mọi giao dịch gắn với đúng Đại lý sở hữu.

#### 4.2.2 Danh mục sản phẩm (Product Catalog)

Cho phép Đại lý xem danh sách sản phẩm hiện có của khách hàng (DMC).

> _› Diễn giải:_ Đại lý xem/tra cứu danh mục sản phẩm/dịch vụ du lịch (tour, dịch vụ) để làm cơ sở gửi yêu cầu báo giá. Ở MVP là danh mục để tham khảo, chưa gắn công cụ định giá.

#### 4.2.3 Quản lý Báo giá (Quote Management)

> Đây là **module trọng tâm** của MVP.

Cho phép Đại lý:
- Gửi yêu cầu báo giá
- Theo dõi trạng thái báo giá
- Nhận kết quả báo giá
- Xác nhận hoặc từ chối báo giá

Sales thực hiện xử lý báo giá trên **CRM hoặc hệ thống nội bộ**. Portal chỉ đóng vai trò là **kênh giao tiếp** với Đại lý.

> _› Diễn giải:_ Đây là bước "hỏi giá — chào giá" **trước khi** chốt đơn, thay cho việc trao đổi qua Email/Excel. Đại lý gửi yêu cầu (sản phẩm, ngày, số khách, yêu cầu riêng…), Sales tính giá bên ngoài Portal rồi trả kết quả về. Ở MVP, Portal **không tự tính giá** — chỉ truyền yêu cầu và hiển thị kết quả. Quote được xác nhận (`Confirmed`) là điều kiện để tạo Booking.

#### 4.2.4 Quản lý Đặt chỗ (Booking Management)

Sau khi Báo giá được xác nhận, hệ thống cho phép tạo **Booking**. Bao gồm:
- Chi tiết đặt chỗ (Booking Detail)
- Trạng thái đặt chỗ (Booking Status)
- Theo dõi đặt chỗ (Booking Tracking)
- Tải chứng từ đặt chỗ (Download Booking Documents)

> _› Diễn giải:_ Booking là **đơn đã chốt** (khác với Quote chỉ là hỏi giá) — phát sinh cam kết và kéo theo hành khách, chứng từ, thanh toán/công nợ. Đại lý theo dõi được chi tiết, trạng thái, tiến trình vận hành và tải các chứng từ liên quan.

#### 4.2.5 Quản lý Hành khách (Passenger Management)

Cho phép Đại lý cung cấp thông tin hành khách. Bao gồm:
- Nhập thủ công (Manual Entry)
- Nhập từ Excel (Excel Import)

#### 4.2.6 Trung tâm Tài liệu (Document Center)

Cho phép Đại lý tải về:
- Chương trình tour (Itinerary)
- Phiếu dịch vụ (Voucher)
- Hóa đơn (Invoice)
- Thư xác nhận (Confirmation Letter)

#### 4.2.7 Quản trị Giá & Công nợ (Pricing & Debt Management)

Trong MVP, hệ thống **chưa** triển khai Pricing Engine hay Contract Pricing; tuy nhiên **cần thiết lập nền tảng quản lý thanh toán và công nợ** theo các mô hình kinh doanh thực tế của Đại lý.

Hệ thống hỗ trợ **3 hình thức thanh toán** chính:

**a) Booking trả tiền ngay (Immediate Payment)**
Áp dụng cho các booking yêu cầu thanh toán trước khi tour khởi hành. Bao gồm:
- Đặt cọc (Deposit)
- Thanh toán phần còn lại trước ngày khởi hành (Final Payment)

Hệ thống cho phép:
- Ghi nhận trạng thái thanh toán theo từng mốc
- Theo dõi tình trạng đã thanh toán / chưa thanh toán
- Hiển thị hạn thanh toán (deadline)

**b) Công nợ theo hạng Đại lý (Credit-based Payment)**
Áp dụng cho các Đại lý được cấp hạn mức công nợ. Bao gồm:
- Thiết lập hạn mức công nợ (Credit Limit)
- Thiết lập thời hạn thanh toán (Payment Term)

Hệ thống cho phép:
- Theo dõi tổng công nợ hiện tại của Đại lý
- Kiểm soát việc tạo booking khi vượt hạn mức / vượt hạn thanh toán (pay-day)
- Hiển thị các khoản công nợ đến hạn / quá hạn

**c) Nạp tiền cấn trừ (Prepaid Wallet)**
Áp dụng cho Đại lý nạp tiền trước và sử dụng dần cho các booking. Bao gồm:
- Ghi nhận số dư tài khoản (Wallet Balance)
- Trừ tiền tự động khi tạo booking
- Theo dõi lịch sử giao dịch

> **Lưu ý phạm vi MVP:**
> - Chỉ tập trung vào **ghi nhận và theo dõi** trạng thái thanh toán / công nợ.
> - **Không** bao gồm tích hợp Cổng thanh toán (Payment Gateway).
> - **Không** bao gồm tự động hóa đối soát tài chính.

#### 4.2.8 Luồng trạng thái (Workflow)

MVP chỉ triển khai **Workflow theo trạng thái**. Ví dụ:

**Báo giá (Quote):**
`New` → `Processing` → `Quoted` → `Confirmed` / `Rejected`

**Đặt chỗ (Booking):**
`Confirmed` → `Operating` → `Completed`

> Luồng phê duyệt (Approval Workflow) **chưa** nằm trong phạm vi MVP.

#### 4.2.9 Thông báo (Notification)

MVP chỉ hỗ trợ **Email Notification**. Bao gồm:
- Tạo báo giá (Quote Created)
- Cập nhật báo giá (Quote Updated)
- Xác nhận đặt chỗ (Booking Confirmed)
- Nhắc thanh toán (Payment Reminder): Đặt cọc / Thanh toán cuối / Công nợ đến hạn

**Không** bao gồm:
- Thông báo đẩy (Push Notification)
- Tin nhắn SMS
- Thông báo trên ứng dụng di động (Mobile Notification)

#### 4.2.10 Tích hợp CRM / eTour

Portal sẽ **đồng bộ** các dữ liệu sau sang CRM / eTour:
- Báo giá (Quote)
- Đặt chỗ (Booking)
- Hành khách (Passenger)
- Trạng thái thanh toán / Thông tin công nợ (Payment Status / Debt Information)

CRM và eTour tiếp tục là nơi Sales xử lý nghiệp vụ.

---

## 5. Giai đoạn 2 — Tự động hóa nghiệp vụ (Business Automation)

### 5.1 Mục tiêu

Sau khi Portal vận hành ổn định, Giai đoạn 2 tập trung **giảm khối lượng thao tác thủ công của Sales** và **tăng khả năng tự phục vụ của Đại lý**.

### 5.2 Phạm vi

Danh sách tính năng (cột **Tính năng** là nội dung gốc; cột **Diễn giải** là giải thích bổ sung — cần khách hàng xác nhận):

| Tính năng | _› Diễn giải_ |
|---|---|
| **Công cụ dựng báo giá động** (Dynamic Quotation Builder) | _Dựng báo giá ngay trên Portal bằng cách ghép các thành phần dịch vụ (khách sạn, xe, tour, vé…), giảm việc Sales tính tay như ở MVP; Portal chuyển từ "kênh truyền tin" sang "công cụ tạo giá"._ |
| **Giá theo hợp đồng** (Contract Pricing) | _Áp bảng giá đã ký theo hợp đồng cho từng Đại lý/nhóm Đại lý (giá net, chiết khấu theo hợp đồng) thay vì giá chung._ |
| **Giá riêng theo từng Đại lý** (Agent-specific Pricing) | _Cùng một sản phẩm nhưng mỗi Đại lý thấy giá khác nhau tùy hạng, thị trường, thỏa thuận riêng — chi tiết hơn Contract Pricing._ |
| **Luồng phê duyệt** (Approval Workflow) | _Bổ sung bước duyệt trước khi đi tiếp (VD: giảm giá quá mức, booking vượt hạn mức phải cấp trên phê duyệt). MVP chưa có phần này._ |
| **Bảng điều khiển** (Dashboard) | _Tổng hợp trực quan số liệu quote/booking/doanh thu/công nợ thay vì tra từng đơn._ |
| **Theo dõi thanh toán nâng cao** | _Tự động nhắc nợ và kiểm soát hạn mức chặt hơn (tự chặn khi vượt hạn mức/quá hạn), nâng cấp phần công nợ thủ công ở MVP._ |
| **Hồ sơ Đại lý** (Agent Profile) | _Đại lý tự quản lý hồ sơ công ty: thông tin liên hệ, hạng (tier), hạn mức, tài liệu…_ |
| **Thu thập thông tin hành khách trực tuyến** (Online Passenger Collection) | _Gửi link để chính hành khách tự điền thông tin online, giảm việc nhập tay/Excel cho Đại lý (so với MVP)._ |
| **Tự động hóa đặt chỗ** (Booking Automation) | _Tự động hóa các bước từ xác nhận quote → tạo booking → sinh chứng từ, giảm thao tác thủ công của Sales._ |
| **Thông báo trên Portal** (Portal Notification) | _Thông báo trong giao diện Portal (in-app), bổ sung bên cạnh Email của MVP._ |

---

## 6. Giai đoạn 3 — Tối ưu hóa nghiệp vụ (Business Optimization)

### 6.1 Mục tiêu

Xây dựng B2B Portal trở thành **nền tảng giao dịch số hoàn chỉnh** giữa khách hàng và toàn bộ hệ sinh thái đối tác.

### 6.2 Phạm vi

Danh sách tính năng (cột **Tính năng** là nội dung gốc; cột **Diễn giải** là giải thích bổ sung — cần khách hàng xác nhận):

| Tính năng | _› Diễn giải_ |
|---|---|
| **Công cụ định giá động** (Dynamic Pricing Engine) | _Định giá tự động, linh hoạt theo thời điểm/cung cầu/quy tắc kinh doanh — mức cao hơn của công cụ dựng báo giá ở Giai đoạn 2._ |
| **Tích hợp API nhà cung cấp** (Supplier API Integration) | _Kết nối API tới nhà cung cấp dịch vụ để lấy dữ liệu và/hoặc đặt dịch vụ tự động._ |
| **Tình trạng phòng khách sạn** (Hotel Availability) | _Tra cứu tình trạng còn/hết phòng khách sạn (thường theo thời gian thực qua API)._ |
| **Tình trạng du thuyền** (Cruise Availability) | _Tra cứu tình trạng chỗ trên du thuyền._ |
| **Tích hợp vé máy bay** (Flight Integration) | _Tra cứu/đặt vé máy bay ngay trong hệ thống._ |
| **Cổng thanh toán** (Payment Gateway) | _Thanh toán trực tuyến qua cổng thanh toán (thẻ, ví điện tử…). MVP loại trừ tính năng này._ |
| **Gợi ý báo giá bằng AI** (AI Quote Recommendation) | _Dùng AI đề xuất phương án/mức báo giá phù hợp cho từng yêu cầu._ |
| **Bảng điều khiển thông minh** (Business Intelligence Dashboard) | _Báo cáo/phân tích chuyên sâu phục vụ ra quyết định (cao hơn Dashboard cơ bản ở Giai đoạn 2)._ |
| **Ứng dụng di động** (Mobile Application) | _App di động cho Đại lý (và/hoặc các vai trò khác)._ |
| **Đa ngôn ngữ** (Multi-language) | _Giao diện/nội dung hỗ trợ nhiều ngôn ngữ theo thị trường._ |
| **Đa tiền tệ** (Multi-currency) | _Giao dịch/báo giá/công nợ theo nhiều loại tiền tệ._ |
| **Phân tích nâng cao** (Advanced Analytics) | _Phân tích chuyên sâu về hành vi, hiệu quả kinh doanh, dự báo…_ |

---

## 7. Lưu ý & Yêu cầu bổ sung cho Agent Portal

Các điểm cần lưu ý khi thiết kế và triển khai hệ thống, **áp dụng xuyên suốt các giai đoạn**:

- **Giao diện theo thị trường:** giao diện hiển thị của các thị trường và nhóm thị trường sẽ khác nhau; chương trình khuyến mãi của thị trường Ấn Độ và Âu/Úc/Mỹ sẽ khác nhau.
- **Phân tầng quản trị (Đại lý):** trong mỗi hãng B2B sẽ có nhiều sub-account cấp dưới, do đó họ có quyền cấp sub-account con.
- **Phân tầng + thiết lập quy tắc hoa hồng theo hạng (tier):** hạng vàng hưởng hoa hồng nhiều hơn hạng bạc…
- **Khuyến mãi tùy biến theo quốc gia:** các chương trình khuyến mãi được customize theo country.
- **Onboarding Đại lý (Agent Onboarding):** đơn giản, dễ dùng cho các Đại lý, kèm bộ handbook hướng dẫn cụ thể.
- **API bán tour cho thị trường ngách (niche):** ví dụ thị trường Nga — cần bộ API kết nối với các nền tảng khác.
- **Affiliate Tracking:** dành cho các bên giới thiệu khách (intro khách); hệ thống cho phép partner theo dõi được tiến trình (progress).
- **Phân tầng quản trị nội bộ:** phân tầng quản trị theo nước, theo vai trò (role).
- **Quy trình tạo tài khoản:** đơn giản, dễ quản lý.
- **API tới người dùng cuối (End User):** có logo hãng của Đại lý và logo khách hàng (white-label).
- **Quản lý khuyến mãi:** theo loại tài khoản (account type) và theo thị trường (tag).

---

*Bản chuẩn hóa tiếng Việt — giữ nguyên trạng nội dung yêu cầu từ tài liệu gốc v1.0 (07/07/2026), chỉ hiệu đính chính tả/định dạng và thống nhất thuật ngữ song ngữ.*
