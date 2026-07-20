using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Work;

namespace TourKit.Api.Pages.TaskPerformance;

// Báo cáo read-only: hiệu suất công việc nội bộ (legacy Tasking) — IWorkTaskService.GetStatsAsync.
// DTO là 1 bản tổng hợp (tổng/theo trạng thái/quá hạn) → hiển thị bằng StatCard, không bảng, không tham số ngày.
[Authorize(Policy = "task.view")]
public class IndexModel : PageModel
{
    private readonly IWorkTaskService _svc;
    public IndexModel(IWorkTaskService svc) => _svc = svc;

    public WorkTaskStatsDto Stats { get; private set; } = new(0, 0, 0, 0, 0, 0);

    public async Task OnGetAsync()
    {
        Stats = await _svc.GetStatsAsync();
    }
}
