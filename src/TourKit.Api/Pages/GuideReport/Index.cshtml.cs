using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;

namespace TourKit.Api.Pages.GuideReport;

// Báo cáo read-only HDV: tổng hợp lịch điều hướng dẫn viên — IGuideAssignmentService.GetStatsAsync.
// Không có service báo cáo HDV riêng → dùng thẻ thống kê điều HDV (tổng/khởi tạo/đang chạy/số HDV) bằng StatCard.
[Authorize(Policy = "guide.view")]
public class IndexModel : PageModel
{
    private readonly IGuideAssignmentService _svc;
    public IndexModel(IGuideAssignmentService svc) => _svc = svc;

    public GuideAssignmentStatsDto Stats { get; private set; } = new(0, 0, 0, 0);

    public async Task OnGetAsync()
    {
        Stats = await _svc.GetStatsAsync();
    }
}
