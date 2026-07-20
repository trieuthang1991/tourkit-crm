using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;

namespace TourKit.Api.Pages.VehicleReport;

// Báo cáo read-only Xe: tổng hợp lịch điều xe — IVehicleAssignmentService.GetStatsAsync.
// Không có service báo cáo xe riêng → dùng thẻ thống kê điều xe (tổng/khởi tạo/đang chạy/số xe) bằng StatCard.
[Authorize(Policy = "vehicle.view")]
public class IndexModel : PageModel
{
    private readonly IVehicleAssignmentService _svc;
    public IndexModel(IVehicleAssignmentService svc) => _svc = svc;

    public VehicleAssignmentStatsDto Stats { get; private set; } = new(0, 0, 0, 0);

    public async Task OnGetAsync()
    {
        Stats = await _svc.GetStatsAsync();
    }
}
