using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Services;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Crm;
using TourKit.Application.Crm.Dtos;
using TourKit.Application.Reports;
using TourKit.Application.Reports.Dtos;

namespace TourKit.Api.Pages.Dashboard;

// Tổng quan điều hành — bám ĐỦ các khối bản cũ (web/src/features/reports/CeoAnalytics.tsx):
// thao tác nhanh · 12 KPI chia 3 nhóm (Doanh thu&Cơ hội · Chi phí&Công nợ · Lợi nhuận&Hiệu quả) ·
// trạng thái hợp đồng · phễu bán hàng · dòng tiền theo phương thức · lịch hẹn CSKH ·
// hiệu suất theo CHI NHÁNH · vinh danh sales · top khách hàng · lịch khởi hành.
// Số liệu THẬT từ IReportService/IBookingService — không dùng dữ liệu giả của demo js.
[Authorize(Policy = "report.dashboard.view")]
public class IndexModel : PageModel
{
    /// <summary>Số dòng mỗi bảng tóm tắt — thẻ tổng quan là chỗ liếc nhanh, không phải bảng đầy đủ.</summary>
    private const int TopRows = 8;

    private readonly IReportService _reports;
    private readonly IBookingService _booking;
    private readonly ICustomerCareService _cares;
    private readonly UserDirectory _users;

    public IndexModel(IReportService reports, IBookingService booking, ICustomerCareService cares, UserDirectory users)
    {
        _reports = reports;
        _booking = booking;
        _cares = cares;
        _users = users;
    }

    public DashboardSummaryDto Summary { get; private set; } = new(0, 0, 0, 0, 0, 0, 0, 0);
    public KpiSummaryDto Kpi { get; private set; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    public OrderStatsDto Orders { get; private set; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    public IReadOnlyList<CashFlowRowDto> CashFlow { get; private set; } = [];
    public IReadOnlyList<TurnoverByDepartmentRowDto> ByDepartment { get; private set; } = [];
    public IReadOnlyList<TurnoverByBranchRowDto> ByBranch { get; private set; } = [];
    public IReadOnlyList<TopCustomerRowDto> TopCustomers { get; private set; } = [];
    public IReadOnlyList<CustomerCareDto> Cares { get; private set; } = [];

    /// <summary>Top sales theo doanh thu, kèm tên nhân viên đã tra sẵn (bản cũ: "Vinh danh chiến binh sales").</summary>
    public IReadOnlyList<(string Name, decimal Turnover, decimal Commission)> TopSales { get; private set; } = [];

    /// <summary>Tổng hoa hồng toàn hệ — KPI nhóm 3 của bản cũ.</summary>
    public decimal CommissionTotal { get; private set; }

    /// <summary>Lợi nhuận THỰC TẾ = tiền đã thu − tiền đã chi (khác lợi nhuận gộp trên sổ).</summary>
    public decimal ActualProfit => Summary.TotalReceived - Summary.TotalPaid;

    public bool CanCare => User.HasClaim("perm", "care.view");
    public bool CanDeparture => User.HasClaim("perm", "departure.view");
    public bool CanBooking => User.HasClaim("perm", "booking.view");
    public bool CanQuote => User.HasClaim("perm", "quote.view");
    public bool CanCustomer => User.HasClaim("perm", "customer.view");
    public bool CanLead => User.HasClaim("perm", "lead.view");
    public bool CanTask => User.HasClaim("perm", "task.view");

    public static string CareStatusLabel(int s) => s switch
    {
        1 => "Đang xử lý",
        2 => "Hoàn thành",
        3 => "Huỷ",
        _ => "Chờ xử lý",
    };

    public static string CareStatusColor(int s) => s switch
    {
        1 => "info",
        2 => "success",
        3 => "danger",
        _ => "warning",
    };

    public async Task OnGetAsync()
    {
        Summary = await _reports.GetDashboardAsync();
        Kpi = await _reports.GetKpiSummaryAsync();
        Orders = await _booking.GetOrderStatsAsync();
        CashFlow = await _reports.GetCashFlowAsync();
        ByDepartment = await _reports.GetTurnoverByDepartmentAsync();
        ByBranch = await _reports.GetTurnoverByBranchAsync();
        TopCustomers = await _reports.GetTopCustomersAsync(TopRows);

        // Hoa hồng theo nhân viên: dùng cho cả KPI tổng hoa hồng lẫn bảng vinh danh sales.
        var commission = await _reports.GetCommissionByUserAsync();
        CommissionTotal = commission.Sum(c => c.CommissionAmount);

        var names = await _users.NamesAsync();
        TopSales = commission
            .OrderByDescending(c => c.Turnover)
            .Take(5)
            .Select(c => (
                Name: names.TryGetValue(c.UserId, out var n) ? n : "(đã xoá)",
                c.Turnover,
                Commission: c.CommissionAmount))
            .ToList();

        if (CanCare)
        {
            Cares = (await _cares.ListAsync(1, TopRows, new CustomerCareListFilter())).Items;
        }
    }
}
