# Handoff: TourKit CRM — Frontend mới (Refined, AntD-free)

## Overview
Bộ frontend mới cho **TourKit CRM** (CRM/điều hành tour đa tenant). Mục tiêu: thay lớp giao diện Ant Design hiện tại bằng **component tự dựng** theo hệ thiết kế "Refined" — rail sáng, bảng màu trung tính, viền hairline, mật độ cao, chuyên nghiệp. **Giữ nguyên bố cục, luồng, menu và toàn bộ nghiệp vụ/endpoint hiện có** — chỉ đổi lớp trình bày + component.

Phạm vi bản thiết kế này: **App shell** (sidebar 19 nhóm + topbar) và **4 màn** đại diện đủ mọi pattern dùng lại cho các màn còn lại:
1. Tổng quan / CEO Analytics (`/workspace`, `/dashboard`)
2. Đơn hàng / LKH (`/orders`, `/departures`)
3. Data khách hàng (`/customers`)
4. Tài chính / Phiếu thu (`/receipts`)

## About the Design Files
Các file trong gói này là **tài liệu tham chiếu thiết kế viết bằng HTML** (prototype thể hiện look & behavior), **không phải code production để copy nguyên**. Nhiệm vụ: **tái dựng thiết kế này trong chính codebase `web/` hiện có** (React 18 + TypeScript + Vite + React Query + react-router + react-hook-form + zod) theo pattern sẵn có của dự án, nhưng **bỏ dependency `antd` và `@ant-design/icons`**, thay bằng component tự dựng + icon font.

- `TourKit CRM.dc.html` — prototype đầy đủ 4 màn + điều hướng (mở trực tiếp bằng trình duyệt để xem tương tác chuyển màn).
- `TourKit CRM (static).html` — bản 1 file tự chứa, chạy offline (tiện xem nhanh, không cần server).

> Prototype dùng dữ liệu mẫu (mock). Khi tái dựng, nối lại vào API/React Query hook & zod schema **đang có sẵn** trong `src/features/**` (không đổi contract API).

## Fidelity
**High-fidelity (hifi).** Màu, typography, spacing, bo góc, trạng thái đều là giá trị cuối. Hãy tái dựng pixel-perfect bằng component tự dựng. Mọi giá trị hex/px liệt kê trong "Design Tokens" là chuẩn.

---

## Design Tokens

### Màu
| Token | Hex | Dùng cho |
|---|---|---|
| accent | `#eb5324` | Nút primary, nav active, mã (code), số nhấn |
| accent-hover | `#d94a1e` | hover |
| accent-press | `#c73e17` | active/gradient logo |
| accent-soft | `rgba(235,83,36,0.10)` | nền nav active, chip |
| canvas | `#f6f6f7` | nền vùng nội dung |
| surface | `#ffffff` | card, sidebar, topbar |
| heading | `#17181c` | tiêu đề trang, số lớn, tên |
| text-strong | `#33343a` | ô bảng nhấn (tên KH, tổng thu) |
| body | `#56575f` | chữ thường |
| muted | `#a9aab0` | metadata, placeholder, header bảng |
| muted-2 | `#8a8b93` | nhãn phụ dưới số |
| line | `#f2f2f4` / `#f4f4f5` | kẻ hàng bảng, viền mảnh |
| border | `#e2e2e5` | viền input/nút ghost/card |
| header-bg | `#fafafa` | nền header bảng, tfoot |

### Semantic (số liệu tài chính, trạng thái)
| Token | Hex | Soft bg |
|---|---|---|
| success | `#1f9d57` | `rgba(31,157,87,0.13)` |
| warning | `#d9821f` | `rgba(217,130,31,0.14)` |
| info/blue | `#2f6bd6` | `rgba(47,107,214,0.12)` / `#e8f0ff` |
| danger | `#d1494a` | `rgba(209,73,74,0.13)` |
| purple (đối tác) | `#7a5af0` | `rgba(122,90,240,0.10)` |

### Seat pills (cột "Khách (chỗ)")
Tổng `#2f6bd6`/`#e8f0ff` · Giữ `#b7791f`/`#fdf3e0` · Bán `#1f9d57`/`#e7f7ee` · Còn `#d1494a`/`#fbeae9`.

### Typography
- Sans: **Geist** (400/500/600/700). Đã có `@fontsource-variable/geist` trong repo.
- Mono: **JetBrains Mono** (tabular-nums) cho **mọi số/tiền/mã/ngày**. Đã có `@fontsource-variable/jetbrains-mono`.
- `body { letter-spacing: -0.006em }`; tiêu đề `letter-spacing: -0.02em`.
- Thang cỡ: page title 23/700; card title 14.5/700; section KPI label 10.5/600 UPPERCASE letter-spacing .05–.06em; KPI number 22–23/700 mono; body 13; bảng cell 12.5–13; header bảng 10.5 UPPERCASE muted; pill 11.5/600.

### Icons
Bỏ `@ant-design/icons`. Dùng **Material Symbols Outlined** (`font-variation-settings:'wght' 300`, cỡ 18–22). Map nhóm menu:
`dashboard, storefront, groups, calculate, shopping_cart, hotel, flight, badge, directions_car, assignment, account_balance, trending_up, percent, account_tree, campaign, bar_chart, handshake, settings, history`.
Topbar: `menu_open, search, add, notifications`. Bảng/nav: `chevron_right, expand_more, chevron_left, download, tune, person_add, task_alt`, …
(Có thể thay bằng lucide-react nếu muốn tree-shake; giữ nét stroke mảnh.)

### Hình khối & bóng
- radius: base `8px`, lg `10px`, card `12px`; logo `9px`; pill `20px`.
- Card = viền `1px solid #ececee` + bóng rất nhẹ `0 1px 2px rgba(20,20,40,0.04)` (hairline-first, KHÔNG bóng nặng, KHÔNG dải màu trái như bản cũ).
- Nút primary: bóng accent `0 6px 16px -6px rgba(235,83,36,0.5)`; hover nhấc `translateY(-1px)`, active phẳng lại.

### Spacing
- Content padding: `24px 26px 40px`, max-width `1520px`.
- Grid gap thẻ/card: `14–16px`. Card padding: KPI `15–16px`, head `14px 18px`, cell `11px 12px` (bảng chính) / `12px 18px` (bảng thưa).
- Topbar & sidebar brand cao `60px`; sidebar rộng `252px`, item padding `9px 12px`, radius 8.

---

## App Shell

### Sidebar (rail sáng, 252px, nền #fff, viền phải #ececee)
- Brand: ô logo 32×32 gradient `linear-gradient(135deg,#eb5324,#c73e17)` chữ "T" trắng + "TourKit" 17/700 `#1c1d21`. Kẻ dưới `#f2f2f4`, sticky.
- Nhãn nhóm "ĐIỀU HÀNH" 10.5/700 UPPERCASE `#b3b4ba`.
- 19 mục top-level (đúng thứ tự & nhãn menu hiện tại — xem `AppShell.tsx` cũ): icon 20px `#9a9ba2` + label 13.5 `#56575f` + chevron_right `#c8c9ce`. Hover nền `#f6f6f7`.
- **Active**: nền `rgba(235,83,36,0.10)`, chữ + icon `#eb5324`, weight 600.
- Mục "Đơn hàng / LKH" khi active → chevron đổi `expand_more` và **hiện submenu con** (Tất cả đơn hàng[active] · Tất cả Tour/LKH · Tour FIT · Tour GIT/Combo · LandTour · Visa · Dịch vụ lẻ), item con padding-left 44px, active `#eb5324`/soft.

### Topbar (60px, nền #fff, viền dưới #ececee)
`menu_open` (toggle collapse) · ô search filled `#f4f4f5` radius 9 rộng 340 (placeholder "Tìm kiếm khách hàng, đơn hàng…") · spacer · nút **Tạo nhanh** (primary, icon add) · chuông `notifications` + badge số đỏ · cụm user (tên "Điều hành viên" 13/600 + email 11 muted + avatar tròn 34 gradient).

---

## Screens / Views

### 1. Tổng quan — CEO Analytics
**Purpose:** dashboard điều hành, số liệu realtime. **Route:** `/workspace`, `/dashboard` (component `CeoAnalytics`).
**Layout (dọc):**
- Breadcrumb "Workspace › Tổng quan" + H1 "CEO Analytics" + desc "Dữ liệu kinh doanh thời gian thực".
- Hàng **quick actions**: 6 nút ghost (Tạo đơn / Tạo tour LKH / Tạo báo giá / Tạo Data khách / Tạo cơ hội / Tạo công việc), icon accent.
- **3 nhóm KPI** (mỗi nhóm: section-label UPPERCASE + grid 4 cột thẻ):
  - Doanh thu & Cơ hội: Tổng doanh thu (green) · Doanh thu thực tế (blue) · Phải thu khách hàng (red) · Số đơn (accent). Có link "Xem chi tiết ›".
  - Chi phí & Công nợ: Tổng chi · Tổng chi thực tế · Công nợ NCC (red) · Lợi nhuận gộp (accent).
  - Lợi nhuận & Hiệu quả: Lợi nhuận thực tế · Tiền hoa hồng (accent) · Tỉ lệ thu tiền (blue) · Giá trị TB/đơn.
  - Thẻ KPI: label 10.5 UPPERCASE muted; số 23/700 mono theo màu semantic; link accent 12.
- Grid 1.55fr/1fr: **Dòng tiền theo phương thức** (mỗi phương thức: tên + "Ròng ±x" (green/red); 2 thanh bar "Thu" green / "Chi" red, width = tỉ lệ trên max, track `#f2f2f4` h12 radius6, kèm số phải) | **Phễu bán hàng** (4 stage bar giảm dần: Báo giá blue / Chấp nhận teal / Chuyển đơn warning / Đơn chốt green).
- Grid 1fr/1.55fr: **Trạng thái hợp đồng** (4 số: Đã chốt green / Nháp muted / Huỷ red / Tổng đơn) | **Quản lý lịch hẹn** (bảng 4 dòng: tiêu đề + chi tiết + "Nhắc lúc" mono + pill trạng thái warning/info/success).
- **Hiệu suất theo chi nhánh**: bảng (Chi nhánh / Số đơn / Doanh thu / Thực thu green / Còn thiếu red / Lợi nhuận). Endpoint `/api/v1/reports/turnover-by-branch`.
- Grid 1fr/1fr: **Vinh danh chiến binh sales** (rank badge accent + tên + doanh thu + hoa hồng green; `/reports/commission-by-user`) | **Top khách hàng trung thành** (`/reports/top-customers`).
Endpoints dashboard: `/api/v1/reports/*`, `/api/v1/orders/stats`, `/api/v1/reports/kpi`, `/api/v1/reports/cash-flow`.

### 2. Đơn hàng / LKH
**Purpose:** danh sách đơn/tour. **Route:** `/orders`, `/departures` (`OrdersPage`). **Endpoints:** list `/api/v1/orders`, stats `/api/v1/orders/stats`, filter-options `/api/v1/orders/filter-options`, `/branches`, `/users`, `/departments`, `/market-types`, `/tour-groups`.
**Layout:**
- Header: breadcrumb + H1 "Tất cả đơn hàng" + nút ghost "Xuất Excel" + primary "Tạo đơn hàng".
- **6 KPI card** (label UPPERCASE + số mono): Tổng số đơn · Doanh thu (accent) · Đã thu (green) · Còn nợ (red) · Đã chốt (blue) · Đã huỷ (muted).
- **Filter bar**: ô search (flex-1) + 4 select giả (Tình trạng đơn / NV phụ trách / Chi nhánh / Ngày khởi hành, mỗi cái có chevron) + nút "Lọc" (icon tune). Bản thật giữ đủ ~17 filter như `OrdersPage` cũ, đặt sau "Xem thêm bộ lọc".
- **Chip lọc nhanh thị trường**: "Tất cả"(active accent-soft) + các cấp cha market-type.
- **Segment tabs thanh toán** (nền trắng viền, active = nền accent chữ trắng): Tất cả (1.284) · Chưa thanh toán (268) · Đã cọc (214) · Thanh toán hết (802).
- **Data card** header có chú thích chỗ (● Tổng blue / Giữ gold / Bán green / Còn red). **Bảng** (overflow-x, min-width 1150), cột: `#` · Mã đơn (mono accent) · Khách hàng (strong) · Tour (ellipsis) · Khách(chỗ) = 4 seat pill số · Ngày đi (mono) · Thu tiền (Tổng thu strong / Thực thu green) · Chi tiền (Tổng chi / Thực chi red) · Lợi nhuận (mono, green nếu ≥0 else red, weight 700) · Trạng thái (pill: Đã chốt success / Nháp muted / Đã huỷ danger) · chevron_right. Header UPPERCASE muted nền `#fafafa`.
- **tfoot** "Tổng cộng (trang này)": Tổng thu / Thực thu(green) / Tổng chi / Thực chi(red) / Lợi nhuận(green) / Phải thu.
- **Pagination** hairline: prev + trang (active accent) + next; "Hiển thị 1–7 trong 1.284 đơn".
- **5 thẻ vận hành** (icon-chip trái + số + nhãn): Tổng số tour · Đang chạy (blue) · Sắp chạy (warning) · Hoàn thành (green) · Hủy (red).

### 3. Data khách hàng
**Purpose:** hồ sơ KH, phân nhóm, chăm sóc. **Route:** `/customers` (`CustomersPage`). **Endpoints:** `/api/v1/customers`, `/customers/stats`, `/customers/filter-options`, `/customers/funnel`, `/users`.
**Layout:**
- Header + desc + primary "Thêm khách hàng" (icon person_add).
- **5 thẻ icon-chip** (chip vuông 42 radius11 màu semantic + số + nhãn): Tổng số khách hàng (accent) · Tạo hôm nay (blue) · Tạo trong tháng (warning) · Mua lần đầu (green) · Mua lại nhiều lần (red).
- Filter bar (search "Nhập tên, SĐT, Email…" + 4 select + "Tìm kiếm").
- **Segment tabs**: Tất cả · Cá nhân · Doanh nghiệp · Đối tác · CTV.
- Card **"Chăm sóc khách hàng"**: chip lọc nhanh có count — Mua lần đầu / Khách mua lại / 7·15·30·90 ngày chưa liên hệ.
- **Bảng** (min-width 1050): `#` · Mã KH (mono accent) · Họ và tên · SĐT (mono) · Tỉnh thành · Phân nhóm (pill màu theo nhóm: VIP accent / Doanh nghiệp blue / Đối tác purple / Đã mua green / Quan tâm warning / Tiềm năng muted) · Số lần mua (center) · Doanh thu (mono right) · Loại KH · NV phụ trách. tfoot: Tổng số lần mua / Tổng doanh thu(green).

### 4. Tài chính / Phiếu thu
**Purpose:** duyệt phiếu thu. **Route:** `/receipts` (`ReceiptsListPage`). **Endpoints:** list `/api/v1/receipts`, `/receipts/stats`, actions `POST /api/v1/receipts/{id}/approve|reject`, `/branches`, `/users`.
**Layout:**
- Header + primary "Lập phiếu thu".
- **5 KPI card**: Tổng số phiếu (accent) · Tổng tiền (green) · Chờ duyệt (warning) · Đã duyệt (blue) · Từ chối (red).
- Filter bar (search + Khoảng thời gian / Chi nhánh / NV phụ trách + "Tìm kiếm").
- **Tabs trạng thái**: Tất cả · Chờ duyệt · Đã duyệt · Từ chối (VOUCHER_STATUS).
- **Bảng** (min-width 1080): `#` · Mã phiếu (mono accent) · Mã đơn (mono) · Khách hàng · Số tiền (mono right) · Hình thức · Ngày (mono) · Người nộp · Trạng thái (pill: Chờ duyệt warning / Đã duyệt success / Từ chối danger) · **Thao tác**. tfoot: Tổng tiền(green).
- **Thao tác** hiển thị theo trạng thái & quyền `receipt.approve`: nếu status = Chờ duyệt → nút **Duyệt** (primary nhỏ) + **Từ chối** (viền đỏ); ngược lại → "—". (Bản cũ dùng Popconfirm trước khi gọi API.)

---

## Interactions & Behavior
- **Điều hướng**: click mục sidebar → đổi route (react-router như hiện tại). Prototype mô phỏng bằng state `screen`; bản thật dùng `<Route>` sẵn có trong `app/router.tsx`.
- **Đơn hàng active** → auto mở submenu con + highlight.
- Hover: item nav nền `#f6f6f7`; hàng bảng nền `#fafafa`; nút primary nhấc 1px + bóng accent.
- Filter: search onEnter/onSearch, select onChange → cập nhật query params → React Query refetch (giữ logic `cleanParams`/`clean` hiện có).
- Tabs & chip: controlled, đổi filter và refetch trang 1.
- Duyệt/Từ chối phiếu thu: confirm → `POST .../approve|reject` → invalidate `['receipts-all']` + `['receipts','stats']`.
- Bảng rộng: `overflow-x:auto` trong card (cột cố định STT/Mã trái, hành động phải như bản cũ nếu cần).

## State Management
Giữ nguyên kiến trúc hiện tại: **React Query** cho data (queryKey theo `[resource, 'list', page, filters]`), **useState** cho page/search/filter draft-vs-applied (pattern `draft`→`applied` như `CustomersPage`/`OrdersPage`), **react-hook-form + zod** cho form modal (`CrudFormModal`), **AuthContext.has(perm)** cho phân quyền hiển thị nút/cột. **Không** thêm state lib mới.

## Component library cần dựng (thay AntD)
Tự dựng, không phụ thuộc antd:
- `AppShell` (Sidebar + Topbar), `NavItem`, `SubNav`
- `Button` (primary/ghost/text/danger), `IconButton`, `Icon` (Material Symbols wrapper)
- `Card` / `DataCard` (head + body), `StatCard` (label-top) & `StatCardIcon` (icon-chip), `SectionTitle`
- `Table` (header UPPERCASE, hover, summary/tfoot, sticky col, overflow-x), `Pagination`
- `SegmentTabs`, `Chip`/`FilterChip`, `Pill`/`StatusTag` (tone success/warning/danger/muted/info)
- `SearchInput`, `Select` (custom dropdown), `DateRangePicker`, `Modal`, `Popconfirm`, `Toast/message`
- `SeatTags`, `MoneyCell`, `Bar`/`FunnelBar` (dashboard)
Tất cả style bằng CSS module/vanilla theo tokens ở trên (không Tailwind bắt buộc; repo có Tailwind v4 no-preflight nếu muốn dùng utility).

## Migration notes
1. Gỡ `antd`, `@ant-design/icons` khỏi `package.json`; bỏ `import 'antd/dist/reset.css'` và `<ConfigProvider>` trong `app/providers.tsx`.
2. Thay `shared/ui/antd.ts` (barrel) + `shared/ui/*` bằng các component tự dựng cùng tên/API để **feature pages không phải sửa nhiều**.
3. Đổi `app/theme.ts` (AntD tokens) → file token CSS (`:root { --tk-* }`) theo bảng Design Tokens.
4. Giữ nguyên `features/**/*Api.ts`, `types.ts` (zod), route, permission.
5. Icon: thêm Material Symbols (link Google Fonts hoặc self-host) hoặc `lucide-react`.

## Assets
- Fonts: Geist + JetBrains Mono (đã self-host qua @fontsource trong repo).
- Icons: Material Symbols Outlined (Google Fonts) — thay toàn bộ @ant-design/icons.
- Không có ảnh bitmap; logo là ô gradient chữ "T".

## Files (trong gói này)
- `TourKit CRM.dc.html` — prototype 4 màn + điều hướng (tham chiếu chính).
- `TourKit CRM (static).html` — bản 1 file offline.
- (Tuỳ chọn) ảnh chụp từng màn — hỏi bên dưới.
