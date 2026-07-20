using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Reports;
using TourKit.Application.Reports.Dtos;

namespace TourKit.Api.Pages.Dashboard;

// Tổng quan — bám bố cục Dashboards/CRM của Vuexy (docs/vuexy-reference/Dashboards/CRM.cshtml),
// toàn bộ số liệu THẬT từ IReportService + IBookingService (không dùng data giả của demo js).
[Authorize(Policy = "report.dashboard.view")]
public class IndexModel : PageModel
{
    private readonly IReportService _reports;
    private readonly IBookingService _booking;
    public IndexModel(IReportService reports, IBookingService booking)
    {
        _reports = reports;
        _booking = booking;
    }

    public DashboardSummaryDto Summary { get; private set; } = new(0, 0, 0, 0, 0, 0, 0, 0);
    public KpiSummaryDto Kpi { get; private set; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    public OrderStatsDto Orders { get; private set; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    public IReadOnlyList<CashFlowRowDto> CashFlow { get; private set; } = [];
    public IReadOnlyList<TurnoverByDepartmentRowDto> ByDepartment { get; private set; } = [];
    public IReadOnlyList<TopCustomerRowDto> TopCustomers { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Summary = await _reports.GetDashboardAsync();
        Kpi = await _reports.GetKpiSummaryAsync();
        Orders = await _booking.GetOrderStatsAsync();
        CashFlow = await _reports.GetCashFlowAsync();
        ByDepartment = await _reports.GetTurnoverByDepartmentAsync();
        TopCustomers = await _reports.GetTopCustomersAsync(8);
    }
}
