using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Application.Admin;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;
using TourKit.Shared.Enums;

namespace TourKit.Api.Pages.Orders;

// Danh sách đơn: DataTables SERVER-SIDE (không get-all) — mỗi lần chỉ tải đúng 1 trang.
[Authorize(Policy = "booking.view")]
public class IndexModel : TkListPageModel
{
    private readonly IBookingService _svc;
    private readonly IUserAdminService _users;
    public IndexModel(IBookingService svc, IUserAdminService users)
    {
        _svc = svc;
        _users = users;
    }

    public OrderStatsDto Stats { get; private set; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

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

    public async Task OnGetAsync() => Stats = await _svc.GetOrderStatsAsync();

    /// <summary>Nguồn DataTables server-side: chỉ trả đúng 1 trang dữ liệu.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var status = int.TryParse(Request.Query["status"], out var st) ? st : (int?)null;

        var filter = new OrderListFilter(Q: dt.Keyword, Status: status, BookingType: BookingType);
        var result = await _svc.ListOrdersAsync(dt.Page, dt.Size, filter);

        // Tên sales: chỉ tra cho các user xuất hiện trong TRANG hiện tại.
        var salesIds = result.Items.Where(o => o.SalesUserId is not null).Select(o => o.SalesUserId!.Value).ToHashSet();
        var names = salesIds.Count == 0
            ? []
            : (await _users.ListAsync()).Where(u => salesIds.Contains(u.Id)).ToDictionary(u => u.Id, u => u.FullName);

        var stats = await _svc.GetOrderStatsAsync();
        var data = result.Items.Select(o => new
        {
            id = o.Id,
            code = o.Code,
            customerName = o.CustomerName ?? "—",
            tourTitle = o.TourTitle ?? "—",
            departureDate = o.DepartureDate?.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture) ?? "—",
            totalRevenue = o.TotalRevenue,
            amountPaid = o.AmountPaid,
            outstanding = o.Outstanding,
            seats = $"{o.SeatSold}/{o.SeatTotal}",
            salesName = o.SalesUserId is Guid g && names.TryGetValue(g, out var n) ? n : "—",
            statusLabel = StatusLabel(o.Status),
            statusColor = StatusColor(o.Status),
        });

        return DtJson(dt.Draw, stats.Total, result.Total, data);
    }
}
