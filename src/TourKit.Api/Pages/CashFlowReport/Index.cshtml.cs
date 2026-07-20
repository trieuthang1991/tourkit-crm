using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Reports;
using TourKit.Application.Reports.Dtos;

namespace TourKit.Api.Pages.CashFlowReport;

// Báo cáo read-only: dòng tiền theo phương thức thanh toán (IReportService.GetCashFlowAsync).
// Không tham số ngày -> không có filter khoảng ngày.
[Authorize(Policy = "report.cashflow.view")]
public class IndexModel : PageModel
{
    private readonly IReportService _svc;
    public IndexModel(IReportService svc) => _svc = svc;

    public IReadOnlyList<CashFlowRowDto> Items { get; private set; } = [];

    public decimal TotalInflow { get; private set; }
    public decimal TotalOutflow { get; private set; }
    public decimal TotalNet { get; private set; }

    public async Task OnGetAsync()
    {
        Items = await _svc.GetCashFlowAsync();
        TotalInflow = Items.Sum(x => x.Inflow);
        TotalOutflow = Items.Sum(x => x.Outflow);
        TotalNet = Items.Sum(x => x.Net);
    }
}
