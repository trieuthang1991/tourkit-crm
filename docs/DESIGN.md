# Design System: TourKit CRM (Admin nội bộ điều hành tour)

> Dán toàn bộ file này vào **Google Stitch** làm "design system / style guide", rồi prompt từng màn.
> Đây là **admin dashboard nghiệp vụ** (nhiều dữ liệu) — KHÔNG phải web marketing: ưu tiên **rõ ràng,
> nhất quán, mật độ cao**, không hero/asymmetric/cinematic. Nội dung/label bằng **Tiếng Việt**.

## 0. Prompt mẫu cho Stitch (dùng khi tạo từng màn)
> "Design an internal **admin dashboard** screen for a Vietnamese tour-operations CRM, following the
> design system below. Screen: **[TÊN MÀN]**. Dense, business-grade, consistent, low-motion.
> Vietnamese labels. Left dark sidebar + top white navbar already exist — design the **content area** only."

---

## 1. Visual Theme & Atmosphere
Giao diện **admin nghiệp vụ, sạch và chắc tay** — cảm giác như bảng điều khiển vận hành: thoáng vừa đủ
nhưng **mật độ dữ liệu cao** (bảng nhiều cột, thẻ số). Card mềm bo góc, đổ bóng khuếch tán nhẹ trên nền
xám ấm. **Bố cục đối xứng, dự đoán được** (người dùng nghiệp vụ cần sự nhất quán — KHÔNG bố cục lệch/nghệ
thuật). Chuyển động **tiết chế, chức năng** (chỉ hover/skeleton, không hoạt hoạ điện ảnh).
- **Density (mật độ):** 8/10 — Cockpit Dense (bảng dữ liệu, thẻ số).
- **Variance (biến thiên):** 2/10 — Predictable Symmetric (nhất quán toàn app).
- **Motion:** 2/10 — Static Restrained (hover + skeleton, không hơn).

## 2. Color Palette & Roles (tối đa 1 accent)
- **Canvas Xám Ấm** (`#F8F7FA`) — Nền toàn màn nội dung.
- **Surface Trắng** (`#FFFFFF`) — Nền card, navbar, modal.
- **Sidebar Than** (`#333333`) — Nền menu dọc (KHÔNG dùng đen tuyền `#000`).
- **Heading Chàm Đậm** (`#5E5873`) — Tiêu đề, số liệu nổi bật.
- **Body Xám Tím** (`#6E6B7B`) — Chữ thường, mô tả, metadata.
- **Đường Kẻ Thì Thầm** (`#F3F2F7`) — Viền card, header bảng, kẻ ngăn hàng.
- **Brand Đỏ-Cam** (`#EB5324`) — **ACCENT DUY NHẤT**: nút chính, item menu active (gradient + glow nhẹ),
  focus ring, link, số nhấn. (Đây là brand cố định của dự án — giữ nguyên.)
- Semantic (badge/trạng thái): Success `#28C76F` · Warning `#FF9F43` · Info `#00CFE8` · Danger `#EA5455`.
- **Cấm:** đen tuyền `#000000`; tím/xanh neon "AI"; gradient chữ trên tiêu đề lớn; nhiều accent.

## 3. Typography Rules
- **Font chính:** `Roboto` (300/400/500/700) — **brand cố định**, dùng cho cả heading lẫn body.
  (Đây là ràng buộc thương hiệu; không thay bằng font khác.)
- **Số & tiền trong bảng/thẻ (mật độ cao):** `Roboto Mono` (hoặc `JetBrains Mono`) — canh phải, dễ so cột.
- **Heading:** track-tight, phân cấp bằng **đậm nhạt + màu**, KHÔNG bằng cỡ khổng lồ. H4 tiêu đề màn ~20px,
  H5 tiêu đề card ~16px.
- **Body:** 14px, leading thoải mái, màu `#6E6B7B`.
- **Header bảng:** IN HOA, 12px, letter-spacing 0.4px, màu `#6E6B7B`, nền `#F3F2F7`.
- **Cấm:** serif mọi loại (đây là phần mềm UI); `Inter`; cỡ chữ quá lớn kiểu marketing.

## 4. Component Stylings
- **Nút (Button):** phẳng, bo 6px, KHÔNG glow ngoài. Primary = nền đỏ `#EB5324` + bóng đổ rất nhẹ
  `0 2px 6px rgba(235,83,36,.35)`; Secondary = outline/ghost. Active: lún nhẹ (translateY 1px).
- **Card:** bo 8px, **không viền**, bóng khuếch tán `0 4px 24px rgba(34,41,47,.10)`. Header card có kẻ dưới
  `#F3F2F7`. Dùng card cho: thẻ thống kê, khối bảng, khối form.
- **Thẻ thống kê (Stat card):** card + [icon chip vuông bo góc, nền accent-nhạt 12% + icon màu semantic] +
  [số lớn đậm `#5E5873` (mono)] + [nhãn nhỏ `#6E6B7B`]. Hàng 4–5 thẻ đầu màn danh sách.
- **Bảng (Table):** header nền `#F3F2F7` in hoa; hàng kẻ dưới `#F3F2F7`; hover hàng nền `#FAF9FC`;
  cột STT + Mã (accent) fixed trái, cột thao tác fixed phải; **cuộn ngang** khi nhiều cột. Số/tiền canh phải, mono.
- **Badge/Tag trạng thái:** pill bo tròn, nền semantic-nhạt + chữ semantic đậm (vd "Đã duyệt" xanh, "Chờ" cam,
  "Từ chối" đỏ). Nhiều tag (phân nhóm/NV) = cụm pill.
- **Input/Select/Date:** bo 6px, label phía trên, lỗi phía dưới, focus ring accent. Select nhiều giá trị = chip.
- **Toolbar lọc:** hàng ngang gồm [ô Search rộng] + [các Select lọc] + link "Xem thêm bộ lọc" mở panel lọc nâng cao.
- **Loading:** **skeleton** khớp kích thước bảng/thẻ — KHÔNG spinner tròn.
- **Empty state:** minh hoạ nhẹ + 1 câu hướng dẫn tạo dữ liệu — KHÔNG chỉ chữ "No data".
- **Sidebar item active:** nền **gradient đỏ** `linear-gradient(118deg,#EB5324,rgba(235,83,36,.7))` + glow
  `0 0 10px 1px rgba(235,83,36,.5)`, bo 6px; item thường chữ trắng mờ.

## 5. Layout Principles (khung + 3 archetype màn)
**Khung app (đã có, mô tả để Stitch canh nội dung):** Sidebar dọc tối `#333` (260px, gom nhóm menu) bên trái
→ Navbar trắng trên (search + chuông + avatar) → vùng nội dung nền `#F8F7FA`, padding 24px.

**Archetype A — Màn DANH SÁCH** (đa số màn: KH, Đơn hàng, NCC, Phiếu thu/chi…):
1. Header màn: [tiêu đề H4] + [nút "Thêm mới" đỏ, phải].
2. **Hàng thẻ thống kê** (4–5 stat card, grid đều).
3. **Toolbar tìm kiếm/lọc** (search + select + "Xem thêm bộ lọc").
4. **Card bảng dữ liệu** (header card có filter phụ; bảng giàu cột, phân trang + chọn cỡ trang).

**Archetype B — FORM (Modal):** modal trắng bo 8px, tiêu đề + nút đóng; các field 1 cột (label trên, input dưới),
nhóm logic; footer [Huỷ ghost] + [Lưu đỏ]. Field nhiều giá trị dùng select-tags.

**Archetype C — Dashboard "Bàn làm việc":** grid nhiều khối card:
[Hồ sơ nhân viên + donut Tỉ lệ công việc] · [Feed Thông báo] · [Widget Công nợ khách (xếp hạng)] ·
[Thanh nút tạo nhanh] · [Lịch tháng khởi hành] · [Bảng "Công việc của tôi" + tab].

**Nguyên tắc:** CSS Grid, không `calc()` %; container max-width; **< 768px collapse 1 cột**; không phần tử chồng
lấn; không "3 card ngang bằng nhau" kiểu marketing (dùng grid nghiệp vụ thực).

## 6. Motion & Interaction (tiết chế)
- Hover: đổi nền/nhấc bóng rất nhẹ. Focus: ring accent.
- Chuyển route/mở modal: fade + rời 4px, ~150ms, ease-out. KHÔNG spring nảy, KHÔNG cascade điện ảnh.
- Loading: skeleton shimmer. Chỉ animate `transform`/`opacity`.
- KHÔNG micro-loop vô hạn, KHÔNG hoạt hoạ trang trí (đây là công cụ vận hành, ưu tiên tốc độ đọc).

## 7. Anti-Patterns (CẤM)
- KHÔNG emoji trong UI thật.
- KHÔNG đen tuyền `#000`; KHÔNG neon/glow tím-xanh kiểu AI; KHÔNG nhiều accent.
- KHÔNG serif; KHÔNG `Inter`; KHÔNG cỡ chữ marketing khổng lồ.
- KHÔNG hero section, KHÔNG bố cục lệch nghệ thuật (đây là admin — cần đối xứng, nhất quán).
- KHÔNG "3 card ngang bằng nhau" trang trí; KHÔNG chồng lấn phần tử.
- KHÔNG spinner tròn (dùng skeleton); KHÔNG "No data" trơ.
- KHÔNG tên giả ("John Doe", "Acme"); dùng dữ liệu VN thực tế ("Nguyễn Văn A", "Công ty Du lịch Xanh", "GIT_1381").
- KHÔNG số tròn giả ("99.99%"); dùng số nghiệp vụ thật ("22,036 khách", "141,261,870 đ").
- KHÔNG sáo ngữ marketing ("Elevate", "Seamless"); ngôn ngữ nghiệp vụ tiếng Việt trực tiếp.
- KHÔNG link ảnh Unsplash hỏng; avatar dùng chữ cái tròn màu accent.
