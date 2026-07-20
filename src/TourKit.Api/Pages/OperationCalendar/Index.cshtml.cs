using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;

namespace TourKit.Api.Pages.OperationCalendar;

// READ-ONLY: "Lịch điều hành" liệt kê các chuyến (TourDeparture) theo ngày khởi hành qua IDepartureService.
// Việc điều HDV/xe cho từng chuyến làm ở màn Lịch điều HDV / Lịch điều xe — màn này chỉ là lịch xem theo ngày.
[Authorize(Policy = "departure.view")]
public class IndexModel : PageModel
{
    private readonly IDepartureService _svc;
    public IndexModel(IDepartureService svc) => _svc = svc;

    public IReadOnlyList<DepartureDto> Items { get; private set; } = [];
    public DepartureStatsDto Stats { get; private set; } = new(0, 0, 0, 0);

    public static string StatusLabel(DepartureDto d) => d.IsClosed ? "Đã đóng" : "Đang mở";
    public static string StatusColor(DepartureDto d) => d.IsClosed ? "secondary" : "success";

    public async Task OnGetAsync()
    {
        Stats = await _svc.GetStatsAsync();
        Items = (await _svc.ListAsync(1, 1000)).Items
            .OrderBy(d => d.DepartureDate ?? DateTimeOffset.MaxValue)
            .ToList();
    }
}
