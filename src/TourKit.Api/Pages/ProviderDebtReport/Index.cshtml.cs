using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Reports;
using TourKit.Application.Reports.Dtos;

namespace TourKit.Api.Pages.ProviderDebtReport;

// Báo cáo read-only: công nợ phải trả NCC + phân tuổi nợ (IReportService.GetProviderDebtAsync).
// Không tham số ngày -> không có filter khoảng ngày.
[Authorize(Policy = "report.providerdebt.view")]
public class IndexModel : PageModel
{
    private readonly IReportService _svc;
    public IndexModel(IReportService svc) => _svc = svc;

    public IReadOnlyList<ProviderDebtRowDto> Items { get; private set; } = [];

    public decimal TotalCost { get; private set; }
    public decimal TotalPaid { get; private set; }
    public decimal TotalOutstanding { get; private set; }

    public async Task OnGetAsync()
    {
        Items = await _svc.GetProviderDebtAsync();
        TotalCost = Items.Sum(x => x.TotalCost);
        TotalPaid = Items.Sum(x => x.Paid);
        TotalOutstanding = Items.Sum(x => x.Outstanding);
    }
}
