using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Reports;
using TourKit.Application.Reports.Dtos;

namespace TourKit.Api.Pages.TourTypeReport;

// Báo cáo read-only: thu–chi theo loại tour (IReportService.GetMoneyByTourTypeAsync).
// Không tham số ngày -> không có filter khoảng ngày.
[Authorize(Policy = "report.turnover.view")]
public class IndexModel : PageModel
{
    private readonly IReportService _svc;
    public IndexModel(IReportService svc) => _svc = svc;

    public IReadOnlyList<MoneyByTourTypeRowDto> Items { get; private set; } = [];

    public decimal TotalNetRevenue { get; private set; }
    public decimal TotalCost { get; private set; }
    public decimal TotalProfit { get; private set; }

    public async Task OnGetAsync()
    {
        Items = await _svc.GetMoneyByTourTypeAsync();
        TotalNetRevenue = Items.Sum(x => x.NetRevenue);
        TotalCost = Items.Sum(x => x.Cost);
        TotalProfit = Items.Sum(x => x.Profit);
    }
}
