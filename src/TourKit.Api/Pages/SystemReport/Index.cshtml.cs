using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Reports;
using TourKit.Application.Reports.Dtos;

namespace TourKit.Api.Pages.SystemReport;

// Báo cáo read-only tổng hợp: KPI phễu kinh doanh (IReportService.GetKpiSummaryAsync) + Top khách hàng (GetTopCustomersAsync).
// KHÁC FinanceReport (dùng GetDashboardAsync) — màn này tập trung phễu báo giá→đơn→thu + top khách.
[Authorize(Policy = "report.turnover.view")]
public class IndexModel : PageModel
{
    private const int TopCustomers = 10;

    private readonly IReportService _svc;
    public IndexModel(IReportService svc) => _svc = svc;

    public KpiSummaryDto Kpi { get; private set; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    public IReadOnlyList<TopCustomerRowDto> TopList { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Kpi = await _svc.GetKpiSummaryAsync();
        TopList = await _svc.GetTopCustomersAsync(TopCustomers);
    }
}
