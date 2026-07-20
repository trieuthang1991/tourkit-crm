using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Reports;
using TourKit.Application.Reports.Dtos;

namespace TourKit.Api.Pages.CommissionByMilestoneReport;

// Báo cáo read-only: hoa hồng theo cột mốc/bậc thang (IReportService.GetCommissionByMilestoneAsync).
// Method NHẬN tham số ngày (from/to) -> có filter khoảng ngày (flatpickr .tk-date, form GET submit lại trang).
[Authorize(Policy = "report.commission.view")]
public class IndexModel : PageModel
{
    private readonly IReportService _svc;
    public IndexModel(IReportService svc) => _svc = svc;

    [BindProperty(SupportsGet = true)]
    public DateTime? From { get; set; }

    [BindProperty(SupportsGet = true)]
    public DateTime? To { get; set; }

    public IReadOnlyList<CommissionByMilestoneRowDto> Items { get; private set; } = [];

    public decimal TotalTurnover { get; private set; }
    public decimal TotalProfit { get; private set; }
    public decimal TotalCommissionByProfit { get; private set; }

    public string? FromValue => From?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    public string? ToValue => To?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public async Task OnGetAsync()
    {
        DateTimeOffset? from = From.HasValue ? new DateTimeOffset(From.Value, TimeSpan.Zero) : null;
        DateTimeOffset? to = To.HasValue ? new DateTimeOffset(To.Value, TimeSpan.Zero) : null;

        Items = await _svc.GetCommissionByMilestoneAsync(from, to);
        TotalTurnover = Items.Sum(x => x.Turnover);
        TotalProfit = Items.Sum(x => x.Profit);
        TotalCommissionByProfit = Items.Sum(x => x.CommissionByProfit);
    }
}
