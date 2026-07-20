using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.B2B;
using TourKit.Application.B2B.Dtos;

namespace TourKit.Api.Pages.AgentBookings;

// List-only (read-only): service chỉ có CreateFromQuoteAsync (tạo từ quote đã Confirmed — nhiều FK)
// và UpdateStatusAsync/AddPassenger; KHÔNG có Create đơn giản hay Delete → bỏ nút thêm/sửa/xoá.
[Authorize(Policy = "agentquote.view")]
public class IndexModel : PageModel
{
    private readonly IAgentBookingService _svc;
    public IndexModel(IAgentBookingService svc) => _svc = svc;

    public IReadOnlyList<AgentBookingSummaryDto> Items { get; private set; } = [];

    public static string StatusLabel(int s) => s switch
    {
        0 => "Chờ",
        1 => "Đã xác nhận",
        2 => "Đã huỷ",
        3 => "Hoàn tất",
        _ => "—",
    };

    public static string StatusColor(int s) => s switch
    {
        1 => "info",
        2 => "secondary",
        3 => "success",
        _ => "warning",
    };

    public async Task OnGetAsync() => Items = (await _svc.ListAsync(1, 1000)).Items;
}
