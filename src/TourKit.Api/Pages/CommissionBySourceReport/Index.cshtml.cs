using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TourKit.Api.Pages.CommissionBySourceReport;

// PLACEHOLDER: IReportService CHƯA có method hoa hồng theo NGUỒN (source).
// Hiện chỉ có GetCommissionByUserAsync (theo nhân viên) và GetCommissionByMilestoneAsync (theo cột mốc).
// Khi có method + DTO phù hợp thì mới nối dữ liệu — tuyệt đối không bịa số.
[Authorize(Policy = "report.commission.view")]
public class IndexModel : PageModel
{
    public void OnGet()
    {
    }
}
