using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Reports;
using TourKit.Application.Reports.Dtos;

namespace TourKit.Api.Pages.CustomerDebtReport;

// Báo cáo read-only: công nợ khách theo đơn hàng (IReportService.GetOrderDebtAsync).
// Không tham số ngày -> không có filter khoảng ngày.
[Authorize(Policy = "report.debt.view")]
public class IndexModel : PageModel
{
    private readonly IReportService _svc;
    public IndexModel(IReportService svc) => _svc = svc;

    public IReadOnlyList<OrderDebtRowDto> Items { get; private set; } = [];

    public decimal TotalAmount { get; private set; }
    public decimal TotalPaid { get; private set; }
    public decimal TotalOutstanding { get; private set; }

    public async Task OnGetAsync()
    {
        Items = await _svc.GetOrderDebtAsync();
        TotalAmount = Items.Sum(x => x.Total);
        TotalPaid = Items.Sum(x => x.Paid);
        TotalOutstanding = Items.Sum(x => x.Outstanding);
    }
}
