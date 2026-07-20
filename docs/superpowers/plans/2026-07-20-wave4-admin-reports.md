# Wave 4 — Quản trị & Báo cáo nâng cao (plan chi tiết theo màn)

> Điều kiện: Wave 1-2 xong (báo cáo cần data đơn/thu chi thật). Chuẩn chung như các wave trước.

## 4.1 Thành viên (Users) `/Users` — M
List + offcanvas (email/tên/phòng ban/chức danh/chi nhánh/active) + gán Roles (multi Select2) + đổi mật khẩu (admin đặt lại). Perm `user.view`.
**Nghiệm thu:** tạo NV mới → login được, thấy đúng menu theo quyền role.

## 4.2 Vai trò & quyền (Roles) `/Roles` — M
List role + **ma trận quyền** (bảng permission theo nhóm module, checkbox — RbacStore đã có hard-delete bảng nối). Lưu AJAX theo role.
**Nghiệm thu:** bỏ perm `customer.view` của role test → user role đó mất menu "Data khách hàng" + bị chặn trang (403 → redirect).

## 4.3 Gói dịch vụ (Billing) `/Billing` — M
Hiện gói hiện tại + hạn + danh sách Plan (card so sánh) + lịch sử. SubscriptionGuardMiddleware đã chặn hết hạn — màn này chỉ hiển thị/nâng cấp.
**Nghiệm thu:** tenant hết hạn thấy banner + bị chặn đúng luồng.

## 4.4 Hoa hồng — Thiết lập (CommissionRules) `/CommissionRules` — M
List rule (%/mức theo NV/phòng/loại tour) + offcanvas. Soi hệ cũ cách áp rule.
**Nghiệm thu:** rule mới ảnh hưởng panel Hoa hồng của đơn (Wave 1.7).

## 4.5 Chính sách bậc thang (CommissionCampaigns) `/CommissionCampaigns` — **L**
Soi kỹ React `commission/CommissionCampaignsPage.tsx` (14.8K) + hệ cũ: campaign + bảng CommissionTier (bậc doanh số → %) + gán CommissionCampaignUser. Form trang riêng (bảng bậc thêm/xoá dòng như QuoteLines).
**Nghiệm thu:** campaign 3 bậc; NV đạt bậc 2 → báo cáo HH tính đúng % bậc 2.

## 4.6 Cụm báo cáo (ReportsController — 8 màn M, cùng khung)
Khung chung: filter (kỳ/chi nhánh/NV) + bảng số + chart (`tk.chart`) + nút export (endpoint export server-side đã có từ P1). Làm lần lượt:
| Màn | Route | Leaf |
|---|---|---|
| Doanh thu | /Reports/Turnover | rp-money |
| Theo phòng ban/NV | /Reports/TurnoverByDepartment | rp-seller, pj-perf |
| Thu chi theo loại tour | /Reports/MoneyByTourType | rp-tourtype |
| Dòng tiền | /Reports/CashFlow | fi-cashflow |
| Công nợ khách | /Reports/OrderDebt | fi-debt-c |
| Công nợ NCC | /Reports/ProviderDebt (+modal giao dịch NCC) | fi-debt-p |
| HH theo nguồn | /Reports/CommissionByUser | hh-source |
| HH theo cột mốc | /Reports/CommissionByMilestone | hh-milestone |
| KPI | /Reports/Kpi | kpi-config |
**Nghiệm thu mỗi màn:** số khớp SQL tay đối chiếu 1 case; export tải được file.

## 4.7 Dashboard (CeoAnalytics) `/Dashboard` — **L**
Thay placeholder: soi React `reports/DashboardPage` + `CeoAnalytics` (16.9K) — chọn đúng các widget hệ cũ có (doanh thu tháng, phễu khách, top NV, đơn gần đây, chart xu hướng). Grid card Vuexy + `tk.chart`.
**Nghiệm thu:** số liệu khớp các report 4.6.

## 4.8 Bàn làm việc (Workspace) `/Workspace` — **L**
Soi React `workspace/WorkspacePage` (17.8K): widget việc của tôi (WorkTasks) + lịch hẹn hôm nay (CustomerCares) + phiếu chờ duyệt (Receipts/Payments) + bảng tin (Posts) + TaskDonut. LƯU Ý: `/Workspace` ≠ `/Dashboard` (2 màn riêng — memory `refined-migration-state`).
**Nghiệm thu:** mỗi widget click đi đúng màn đích.

## 4.9 Dự án & Công việc `/Workflows` (M) + `/Workflows/Board/{id}` (**L Kanban**) + `/WorkTasks` (M)
List dự án; **Kanban board** — viết `tk.kanban` bằng **sortablejs có sẵn Vuexy** (cột = WorkflowSection, kéo thả đổi section/status, AJAX cập nhật); list việc + assignee + hạn.
**Nghiệm thu:** kéo task giữa 2 cột → reload vẫn đúng vị trí; quá hạn đổi màu.

## Chốt Wave 4
Toàn bộ nhóm menu Báo cáo/KPIs/Hoa hồng/Dự án/Cài đặt hết trỏ `#`. Test xanh.
