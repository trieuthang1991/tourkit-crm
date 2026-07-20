using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Application.Admin;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;
using TourKit.Shared.Enums;

namespace TourKit.Api.Pages.Orders;

[Authorize(Policy = "booking.view")]
public class IndexModel : PageModel
{
    private readonly IBookingService _svc;
    private readonly IUserAdminService _users;
    public IndexModel(IBookingService svc, IUserAdminService users)
    {
        _svc = svc;
        _users = users;
    }

    public IReadOnlyList<OrderDto> Items { get; private set; } = [];
    public OrderStatsDto Stats { get; private set; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    public Dictionary<Guid, string> SalesNames { get; private set; } = [];

    [BindProperty(SupportsGet = true, Name = "bookingType")] public int? BookingType { get; set; }

    // Loại tour (BookingType): 0 FIT · 1 GIT · 2 LandTour/Combo · 3 Booking phòng · 4 Dịch vụ lẻ · 5 Visa · 6 Xe.
    private static readonly string[] TypeLabels = ["Tour FIT", "Tour GIT/Combo", "LandTour", "Booking phòng", "Dịch vụ lẻ", "Visa", "Xe"];
    public string TypeLabel => BookingType is int t && t >= 0 && t < TypeLabels.Length ? TypeLabels[t] : "Tất cả";

    public static string StatusLabel(OrderStatus s) => s switch
    {
        OrderStatus.Draft => "Nháp/Giữ chỗ",
        OrderStatus.Confirmed => "Đã xác nhận",
        OrderStatus.Cancelled => "Đã huỷ",
        OrderStatus.Closed => "Đã tất toán",
        _ => "—",
    };

    public static string StatusColor(OrderStatus s) => s switch
    {
        OrderStatus.Confirmed => "info",
        OrderStatus.Cancelled => "secondary",
        OrderStatus.Closed => "success",
        _ => "warning",
    };

    public async Task OnGetAsync()
    {
        Stats = await _svc.GetOrderStatsAsync();
        var filter = BookingType is int bt ? new OrderListFilter(BookingType: bt) : null;
        Items = (await _svc.ListOrdersAsync(1, 1000, filter)).Items;
        SalesNames = (await _users.ListAsync()).ToDictionary(u => u.Id, u => u.FullName);
    }

    public string SalesName(Guid? id) => id is Guid g && SalesNames.TryGetValue(g, out var n) ? n : "—";
}
