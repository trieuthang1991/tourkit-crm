using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Reports;
using TourKit.Application.Reports.Dtos;

namespace TourKit.Api.Pages.FinanceReport;

// Báo cáo read-only: tổng quan tài chính (doanh thu/thu/chi/công nợ/lợi nhuận) — IReportService.GetDashboardAsync.
// DTO là 1 bản tổng hợp -> hiển thị bằng StatCard, không có bảng, không tham số ngày.
[Authorize(Policy = "report.turnover.view")]
public class IndexModel : PageModel
{
    private readonly IReportService _svc;
    public IndexModel(IReportService svc) => _svc = svc;

    public DashboardSummaryDto Summary { get; private set; } =
        new(0, 0, 0, 0, 0, 0, 0, 0);

    public async Task OnGetAsync()
    {
        Summary = await _svc.GetDashboardAsync();
    }
}
