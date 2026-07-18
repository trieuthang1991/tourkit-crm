# Item A — Báo cáo Hoa hồng theo mốc (Commission by Milestone)

**Ngày:** 2026-07-18 · **Effort:** M · **Trạng thái:** ⏳

## Bối cảnh & quyết định
- UI `CommissionCampaignsPage` + route `/commission-campaigns` + menu **đã hoàn chỉnh** — không cần thêm.
- Phần còn lại: **wire bậc thang vào report**. Đối chiếu legacy (`uspSearchReportCommissionMilestone`): legacy **KHÔNG** sửa report hoa hồng phẳng, mà có **report riêng** "Commission by Milestone". → Ta làm **report mới**, không đụng `commission-by-user` (phẳng) cũ. Đây là "đổi có kiểm soát riêng" gap doc đã ghi.

## Logic gốc (rút từ stored-proc, bỏ tầng gom tour khác model)
Gom theo user trong khoảng ngày → doanh thu / chi / **lợi nhuận**; tìm campaign áp dụng cho user (active, ngày giao khoảng) → **% = bậc chứa profit**; xuất `ComByProfit = profit×%` và `ComByRevenue = doanhthu×%`.

## Thiết kế (bám model tourkit-crm)
- **Query** `GetCommissionByMilestoneAsync(DateTimeOffset? from, DateTimeOffset? to)`:
  1. Orders có `SalesUserId != null`, lọc `CreatedAt ∈ [from,to]` (nếu có).
  2. Gom theo user: `turnover=Σ TotalRevenue`, `cost=Σ OrderCosts.ActualAmount`, `profit=turnover-cost`.
  3. refDate = `to ?? now`. Campaign áp dụng: user là member (`CommissionCampaignUsers`), `Status==0` (đang áp dụng), `StartDate ≤ refDate ≤ EndDate`, chọn `StartDate` mới nhất.
  4. tiers campaign → `rate=TieredRate(profit)`, `comByProfit=TieredCommission(profit)`, `comByRevenue=round(turnover×rate/100,2)`. Không campaign → 0/0/0, campaign null.
- **DTO** `CommissionByMilestoneRowDto(Guid UserId, decimal Turnover, decimal Cost, decimal Profit, decimal CommissionRate, decimal CommissionByProfit, decimal CommissionByRevenue, string? CampaignName)`.
- **API** `GET /api/v1/reports/commission-by-milestone?from=&to=`, `[Authorize(ReportCommissionView)]` (tái dùng, không thêm permission).
- **FE** trang `CommissionByMilestoneReportPage` (DateRange + bảng + ExportButton) + route + menu; map UserId→tên qua user options (như report phẳng).

## Test (TDD, InMemory + ReportQueries trực tiếp — mirror `CommissionByUserReportTests`)
1. User có campaign bậc thang, profit rơi bậc 2 → rate/comByProfit/comByRevenue đúng.
2. User không campaign → rate 0, cả hai commission 0 (vẫn xuất dòng doanh thu/lợi nhuận).
3. Lọc ngày: đơn ngoài [from,to] bị loại.

## Không làm (YAGNI)
checkType departure/checkout, phân trang, phạm vi branch/department (report phẳng hiện cũng chưa có) — để thống nhất, thêm sau nếu cần.
