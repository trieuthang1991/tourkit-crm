# Kế hoạch chuyển Frontend: Razor → React (shadcn/ui + AG Grid)

> Quyết định chủ dự án (2026-08): chuyển **hết** nghiệp vụ Razor sang React; bảng dùng **AG Grid**; bỏ AntD (thấy giao diện chưa ổn); dùng **shadcn/ui + Tailwind** cho phần không phải bảng; **chuyển dần từng module** (giữ Razor production). Backend dùng chung → 2 FE chạy song song.

## Hiện trạng (khảo sát 2026-08)
- **React `web/` KHÔNG rỗng**: ~90 trang feature (AntD 5) — customers, booking(orders/departures), finance(payments/receipts), providers, quotes, invoices, leads, ~12 report, commission, guides, operations calendar, ratings… Nhưng **chưa đầy đủ** so với Razor (18+ màn Razor mới hơn) và **theme AntD chưa đẹp**.
- **Seam trừu tượng đã có** (điểm tựa migration):
  - `web/src/shared/ui/antd.tsx` (~35KB) — shim tập trung AntD, nhiều trang import từ `./antd`.
  - `shared/ui/ResourcePage.tsx` + `useCrudResource.ts` — khung CRUD generic (nhiều màn danh mục sinh từ đây).
  - `shared/ui/*` — StatCard, TableCells, PageHeader, Field(`ui/form`), SegmentTabs…
  - `shared/httpClient.ts` + `shared/api/` + TanStack Query — tầng gọi API (GIỮ NGUYÊN, dùng lại 100%).
- **Tailwind v4 đã cấu hình** (`styles/tailwind.css`, TẮT preflight để không đụng AntD, đã có brand token `--color-brand:#eb5324`). → dựng lớp shadcn song song an toàn.
- Đã cài: `ag-grid-react` + `ag-grid-community` 36.1.0, `@radix-ui/*`, `class-variance-authority`, `clsx`, `tailwind-merge`, `lucide-react`.
- **AG Grid MCP** (`ag-mcp`) đã đăng ký ở `.mcp.json` → dùng `search_docs` để viết code AG Grid v36 đúng API (Theming API + ModuleRegistry) thay vì đoán.

## Chiến lược (leverage seam, không rewrite mù)
1. **Nền dùng chung mới** (`shared/ui2/` shadcn): `cn` util; primitives Button/Card/Input/Select/Dialog/Drawer/Badge/Label; `StatCard`, `PageHeader`, `FilterBar`.
2. **`DataGrid`** — wrapper AG Grid (Community): Infinite Row Model (phân trang **server-side** free), locale VI, theme quartz + accent #eb5324, cột giàu (cell renderer 2 dòng), pinned bottom row (dòng tổng trang), export CSV. *(viết với hỗ trợ ag-mcp)*
3. **`ResourceGrid`** — thay `ResourcePage` trên nền mới: nhận cấu hình cột (định dạng AG Grid) + hook CRUD sẵn (`useCrudResource`). Chuyển khung này → **kéo theo loạt màn danh mục**.
4. **Chuyển theo nhóm, gắn route riêng** (giữ trang AntD cũ tới khi bản mới đạt parity): 🟢 danh mục (rẻ) → 🟡 giao dịch (Orders/Payments/Receipts/ServiceBookings/Providers/Quotes/Invoices/FlightTickets) → 🔴 tổng hợp cuối (Workspace, OperationCalendar, 4 report chưa có API — **bóc API + test đối chiếu số** để không sai nghiệp vụ).
5. **Cắt AntD** khi mọi trang đã chuyển: gỡ `antd`, xóa shim.

## Màn mẫu (làm trước, chốt look & pattern với chủ dự án)
**Khách hàng (`/khach-hang`)** — giàu nhất: stat cards + filter bar + cột ghép + form drawer + export. Dựng bằng DataGrid + shadcn, gọi API `/customers` + `/customers/stats` sẵn có. Chủ dự án duyệt giao diện TRƯỚC khi nhân bản hàng loạt.

## Nguyên tắc giữ đúng nghiệp vụ
- KHÔNG đụng tầng service/API (nghiệp vụ lõi). FE chỉ đổi cách hiển thị + gọi API.
- Màn 🔴 (Workspace/Calendar/report): số liệu phải **khớp bản Razor** → viết test so sánh output trước khi cắt.
- Mỗi nhóm màn: 1 commit, build + `vitest` xanh, chạy thử.

## Việc cần chủ dự án
- **Reload Claude Code** để `ag-mcp` (trong `.mcp.json`) kết nối — sau đó AI dùng MCP viết AG Grid chuẩn version. Khi reload nhớ **đồng ý (trust)** server `ag-mcp` trong `.mcp.json`.
- Duyệt **look & feel** màn Khách hàng mẫu trước khi làm loạt.

## ĐIỂM TIẾP TỤC (sau khi reload, ag-mcp đã nối) — làm ngay
Chủ dự án đã CHỐT (A): rebuild `/khach-hang-v2` thành **mẫu AG Grid CHUẨN, server-native, dễ dùng nhất**. Việc cụ thể:
1. `detect_version` (đã có ag-grid 36.1.0, React) → `search_docs` cho **Infinite Row Model** + **map filterModel/sortModel → datasource getRows** (v36).
2. Sửa `web/src/shared/ui2/data-grid.tsx`: chuyển sang `rowModelType="infinite"` + `datasource.getRows(params)` (params.startRow/endRow/sortModel/filterModel → gọi `/api/v1/customers?page&size&q&customerType&sort…`), **pagination native của AG Grid**, cột **flex tự giãn kín**, sort/filter cột **đẩy server** (đúng trên toàn 3.007 khách), quick-search native.
3. Cập nhật `web/src/features/customers/CustomersGridPage.tsx` dùng datasource thay `rowData` + bỏ pager tự chế.
4. Kiểm hợp đồng API: `/api/v1/customers` nhận tham số sort nào? (xem `CustomersController` + `CustomerService.ListAsync`/`ICustomerService`). Nếu thiếu sort param thì bổ sung ở service (bám filter Razor `CustomerListFilter`).
5. Verify preview (đăng nhập demo `admin@demo.vn` / `Demo@12345`, route `/khach-hang-v2`) → chủ dự án duyệt look → nhân bản pattern sang các màn khác.
- Trạng thái: backend chốt-sổ-hoa-hồng ĐÃ xong (commit 44440c3). Nền shadcn `ui2/` + DataStore first-pass client-side ĐÃ commit (0641d65, 37deec1). Cần thay bằng Infinite Row Model.
