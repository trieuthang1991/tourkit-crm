using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;
using TourKit.Shared.Enums;

namespace TourKit.Api.Pages.ServiceBookings;

// LIST + read-only: CreateServiceBookingDto cần nhiều FK (OrderId/ProviderId/RoomClassId) và DTO không enrich tên
// NCC/đơn → không dựng offcanvas create để tránh bịa lookup. Chỉ hiển thị danh sách.
[Authorize(Policy = "servicebooking.view")]
public class IndexModel : PageModel
{
    private readonly IServiceBookingService _svc;
    public IndexModel(IServiceBookingService svc) => _svc = svc;

    public IReadOnlyList<ServiceBookingDto> Items { get; private set; } = [];

    public static string TypeLabel(ServiceBookingType t) => t switch
    {
        ServiceBookingType.Hotel => "Khách sạn",
        ServiceBookingType.Flight => "Vé máy bay",
        ServiceBookingType.Visa => "Visa",
        ServiceBookingType.Ticket => "Vé tham quan",
        ServiceBookingType.Transfer => "Đưa đón",
        ServiceBookingType.Other => "Khác",
        _ => t.ToString(),
    };

    public async Task OnGetAsync() => Items = (await _svc.ListAsync(1, 1000)).Items;
}
