using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TourKit.Api.Pages.KpiConfig;

// PLACEHOLDER: không có service KPI trong TourKit.Application (đã grep: không có IKpiService/IKPIService).
// Dùng quyền report.dashboard.view (không có quyền KPI riêng). Không bịa dữ liệu/nghiệp vụ.
[Authorize(Policy = "report.dashboard.view")]
public class IndexModel : PageModel
{
    public void OnGet()
    {
    }
}
