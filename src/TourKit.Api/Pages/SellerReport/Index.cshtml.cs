using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Reports;
using TourKit.Application.Reports.Dtos;

namespace TourKit.Api.Pages.SellerReport;

// Báo cáo read-only: hiệu suất/hoa hồng theo nhân viên sales (IReportService.GetCommissionByUserAsync).
// Không tham số ngày -> không có filter khoảng ngày.
[Authorize(Policy = "report.turnover.view")]
public class IndexModel : PageModel
{
    private readonly IReportService _svc;
    public IndexModel(IReportService svc) => _svc = svc;

    public IReadOnlyList<CommissionByUserRowDto> Items { get; private set; } = [];

    public decimal TotalTurnover { get; private set; }
    public decimal TotalProfit { get; private set; }
    public decimal TotalCommission { get; private set; }

    public async Task OnGetAsync()
    {
        Items = await _svc.GetCommissionByUserAsync();
        TotalTurnover = Items.Sum(x => x.Turnover);
        TotalProfit = Items.Sum(x => x.Profit);
        TotalCommission = Items.Sum(x => x.CommissionAmount);
    }
}
