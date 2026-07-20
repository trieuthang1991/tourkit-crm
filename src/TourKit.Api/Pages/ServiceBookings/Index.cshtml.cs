using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Providers;
using TourKit.Shared.Enums;

namespace TourKit.Api.Pages.ServiceBookings;

// Đặt dịch vụ lẻ: DataTables SERVER-SIDE + GIỮ ĐỦ thông tin bản cũ
// (web/src/features/serviceBookings/ServiceBookingsPage.tsx): 6 KPI, tab theo loại dịch vụ,
// 4 tiêu chí lọc (từ khoá · NCC · trạng thái · khoảng ngày bắt đầu), cột ghép Dịch vụ/NCC,
// Thời gian sử dụng (BĐ → KT), Giá trị (thành tiền + SL × đơn giá).
// Vẫn READ-ONLY: Create/Update DTO cần FK OrderId/ProviderId/RoomClassId — không dựng form để tránh bịa lookup.
[Authorize(Policy = "servicebooking.view")]
public class IndexModel : TkListPageModel
{
    private readonly IServiceBookingService _svc;
    private readonly IProviderService _providers;

    public IndexModel(IServiceBookingService svc, IProviderService providers)
    {
        _svc = svc;
        _providers = providers;
    }

    public ServiceBookingStatsDto Stats { get; private set; } = new(0, 0, 0, 0, 0, 0, 0, 0);
    public IReadOnlyList<(Guid Id, string Name)> Providers { get; private set; } = [];

    /// <summary>Tab loại dịch vụ — bám Segmented hệ cũ (giá trị enum, nhãn tiếng Việt).</summary>
    public static readonly (int Value, string Label)[] TypeTabs =
    [
        ((int)ServiceBookingType.Hotel, "Khách sạn"),
        ((int)ServiceBookingType.Flight, "Vé máy bay"),
        ((int)ServiceBookingType.Visa, "Visa"),
        ((int)ServiceBookingType.Ticket, "Vé tham quan"),
        ((int)ServiceBookingType.Transfer, "Đưa đón"),
        ((int)ServiceBookingType.Other, "Khác"),
    ];

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

    public static string TypeColor(ServiceBookingType t) => t switch
    {
        ServiceBookingType.Hotel => "primary",
        ServiceBookingType.Flight => "info",
        ServiceBookingType.Visa => "warning",
        ServiceBookingType.Ticket => "success",
        ServiceBookingType.Transfer => "secondary",
        _ => "secondary",
    };

    public async Task OnGetAsync()
    {
        Stats = await _svc.GetStatsAsync();
        Providers = (await _providers.ListAsync(1, 1000)).Items.Select(p => (p.Id, $"{p.Name} ({p.Code})")).ToList();
    }

    /// <summary>Dựng bộ lọc từ query — đúng các tiêu chí ServiceBookingListFilter hỗ trợ.</summary>
    private ServiceBookingListFilter BuildFilter(string? keyword)
    {
        var q = Request.Query;
        int? I(string k) => int.TryParse(q[k], out var n) ? n : null;
        Guid? G(string k) => Guid.TryParse(q[k], out var g) ? g : null;
        DateTimeOffset? D(string k) => DateTimeOffset.TryParse(q[k], CultureInfo.InvariantCulture, out var d) ? d.ToUniversalTime() : null;

        var typeValue = I("type");
        var type = typeValue is int t && Enum.IsDefined(typeof(ServiceBookingType), t) ? (ServiceBookingType)t : (ServiceBookingType?)null;

        return new ServiceBookingListFilter(
            Q: keyword,
            Type: type,
            ProviderId: G("providerId"),
            OrderId: G("orderId"),
            Status: I("status"),
            DateFrom: D("dateFrom"),
            DateTo: D("dateTo"));
    }

    /// <summary>Nguồn DataTables server-side: chỉ trả đúng 1 trang.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var result = await _svc.ListAsync(dt.Page, dt.Size, BuildFilter(dt.Keyword));

        // Tên NCC: chỉ tra cho NCC xuất hiện trong TRANG hiện tại.
        var providerIds = result.Items.Where(b => b.ProviderId is not null).Select(b => b.ProviderId!.Value).ToHashSet();
        var names = providerIds.Count == 0
            ? []
            : (await _providers.ListAsync(1, 1000)).Items.Where(p => providerIds.Contains(p.Id)).ToDictionary(p => p.Id, p => p.Name);

        var stats = await _svc.GetStatsAsync();
        var data = result.Items.Select(b => new
        {
            id = b.Id,
            code = b.Code,
            typeLabel = TypeLabel(b.Type),
            typeColor = TypeColor(b.Type),
            description = string.IsNullOrWhiteSpace(b.Description) ? "—" : b.Description,
            providerName = b.ProviderId is Guid g && names.TryGetValue(g, out var n) ? n : "—",
            startDateText = b.StartDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "—",
            endDateText = b.EndDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            quantity = b.Quantity,
            unitPrice = b.UnitPrice,
            totalAmount = b.TotalAmount,
            status = b.Status,
        }).ToList();

        // Tổng cộng TRANG HIỆN TẠI.
        var pageSum = new { total = data.Sum(x => x.totalAmount), quantity = data.Sum(x => x.quantity) };

        return new JsonResult(new
        {
            draw = dt.Draw,
            recordsTotal = stats.Total,
            recordsFiltered = result.Total,
            data,
            pageSum,
        });
    }
}
