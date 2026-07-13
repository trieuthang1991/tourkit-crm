# Migration UI: Ant Design → Vuexy (Bootstrap 5 + reactstrap)

> Chủ dự án chốt (2026-07-13): **thay hẳn Ant Design bằng phong cách/component Vuexy** (ThemeForest,
> bản React = Bootstrap 5 + reactstrap + SCSS). Đây là việc LỚN (nhiều tuần, ~60 màn). File này là
> BẢN ĐỒ để làm **tăng dần, KHÔNG phá app** — AntD và Bootstrap **cùng tồn tại** trong lúc migrate.

## Nguyên tắc an toàn (đọc kỹ)
1. **Coexist**: giữ AntD chạy; thêm reactstrap/Bootstrap song song. Đổi TỪNG màn. App luôn build+chạy được.
2. **Cẩn thận Bootstrap "reboot"**: `import 'bootstrap/scss/bootstrap'` reset global (h1-6, a, button, table…) sẽ
   ĐÈ style AntD → làm hỏng hình các màn CHƯA migrate. → **CHƯA import bootstrap global** cho tới khi ĐA SỐ
   màn đã đổi. Trong giai đoạn đầu: chỉ import ở màn đã migrate (CSS module/scoped) hoặc dùng `bootstrap-utilities`
   (không reboot) + class reactstrap. Khi >70% màn xong → bật bootstrap global + gỡ AntD.
3. **Đổi test theo màn**: mỗi màn migrate xong, cập nhật vitest tương ứng. Giữ suite xanh.
4. **Giữ logic**: chỉ đổi lớp trình bày (component/markup/style). KHÔNG đụng hooks TanStack Query, zod schema,
   API, business logic. Data flow y nguyên.

## Stack đã cài (package.json)
`bootstrap`, `reactstrap`, `@popperjs/core`, `sass`. (Vuexy React thật dùng thêm các lib: apexcharts, vuexy SCSS,
perfect-scrollbar… — thêm khi cần theo từng màn.)

## Bản đồ component AntD → reactstrap/Bootstrap
| AntD | Thay bằng |
|---|---|
| `Layout/Sider/Header/Content` | `div` + Vuexy vertical layout (sidebar `.main-menu` + navbar `.header-navbar`) |
| `Menu` (sidebar) | `Nav/NavItem/NavLink` (reactstrap) + accordion nhóm |
| `Card` | `Card/CardHeader/CardBody` (reactstrap) |
| `Table` | `Table` (reactstrap) HOẶC giữ 1 lib bảng (vd `@tanstack/react-table` cho sort/filter/paging) — **khuyên dùng tanstack-table** vì bảng phức tạp (STT/fixed/scroll) |
| `Form` + Field + `CrudFormModal` | `Modal/ModalHeader/ModalBody` + `Form/FormGroup/Label/Input` (reactstrap) + react-hook-form (giữ) |
| `Select` (mode multiple/tags) | `react-select` (Vuexy dùng cái này) |
| `DatePicker` | `react-flatpickr` (Vuexy) |
| `Statistic`/thẻ KPI | Card + markup Vuexy stat |
| `Tag` | `Badge` (reactstrap) |
| `Button/Popconfirm` | `Button` + `sweetalert2` (Vuexy confirm) hoặc Modal xác nhận |
| `message`/`App.useApp` | `react-toastify` (Vuexy) |
| `Tabs` | `Nav tabs` + `TabContent/TabPane` (reactstrap) |

## Thứ tự migrate (đề xuất — làm từ khung ra ngoài)
1. **Foundation**: SCSS theme (biến màu brand #EB5324, radius, font Roboto), toast provider, confirm helper. → `web/src/styles/`.
2. **Shell**: `AppShell` → layout Vuexy (sidebar dọc gom nhóm + navbar). Đây là khung mọi màn.
3. **Component dùng chung**: bảng (tanstack-table wrapper), form modal, field, select/date. → thay `shared/ui/*`.
4. **Màn theo cụm** (giữ nội dung, đổi vỏ): Bàn làm việc → Khách hàng → Đơn hàng → NCC → Phiếu thu/chi → … → catalog.
5. **Dọn**: khi hết màn AntD → gỡ `antd`, bật bootstrap global, xoá theme AntD ở `providers.tsx`.

## Trạng thái hiện tại
- [x] Cài stack (bootstrap/reactstrap/sass).
- [ ] SCSS theme + toast/confirm.
- [ ] Shell Vuexy.
- [ ] Shared components (table/form/field/select).
- [ ] Migrate màn (0/~60).

## Lưu ý bản quyền
Vuexy là template TRẢ PHÍ. Dùng cho dự án đã mua license là hợp lệ. Khi copy SCSS/asset từ repo Vuexy,
giữ trong dự án nội bộ, không public lại code template.
