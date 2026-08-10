using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Customers;
using TourKit.Application.Customers.Dtos;

namespace TourKit.Api.Pages.Customers;

[Authorize(Policy = "customer.view")]
public class DetailsModel : TourKit.Api.Pages.Shared.TkListPageModel
{
    private readonly ICustomerService _service;
    private readonly ICustomerTypeService _types;
    private readonly ICustomerSourceService _sources;
    private readonly ICustomerTagService _tags;
    private readonly IMarketTypeService _markets;
    private readonly TourKit.Application.Booking.IBookingService _booking;
    private readonly TourKit.Application.Crm.ICustomerCareService _cares;
    public DetailsModel(ICustomerService service, ICustomerTypeService types,
        ICustomerSourceService sources, ICustomerTagService tags, IMarketTypeService markets,
        TourKit.Application.Booking.IBookingService booking, TourKit.Application.Crm.ICustomerCareService cares)
    {
        _service = service;
        _types = types;
        _sources = sources;
        _tags = tags;
        _markets = markets;
        _booking = booking;
        _cares = cares;
    }

    public CustomerDto Customer { get; private set; } = default!;
    public string TypeName { get; private set; } = "";

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        Customer = await _service.GetAsync(id);
        await IndexModel.LoadCatalogsAsync(this, _types, _sources, _tags, _markets);
        var types = (IReadOnlyList<CustomerTypeDto>)ViewData["CustomerTypes"]!;
        TypeName = types.FirstOrDefault(t => t.Code == Customer.CustomerType)?.Name ?? "Khách lẻ";
        return Page();
    }

    /// <summary>
    /// Đơn hàng của ĐÚNG khách này — phân trang tại server, lọc tại DB bằng OrderListFilter.CustomerId.
    /// Khách doanh nghiệp có thể có hàng trăm đơn; tải hết về rồi lọc trong bộ nhớ là tự chuốc lấy
    /// một màn hình treo đúng vào lúc hồ sơ dày nhất.
    /// </summary>
    public async Task<IActionResult> OnGetOrdersAsync(Guid id)
    {
        var dt = ParseDataTables();
        var result = await _booking.ListOrdersAsync(dt.Page, dt.Size,
            new TourKit.Application.Booking.Dtos.OrderListFilter(Q: dt.Keyword, CustomerId: id));

        var data = result.Items.Select(o => new
        {
            id = o.Id,
            code = o.Code,
            statusLabel = Orders.IndexModel.StatusLabel(o.Status),
            statusColor = Orders.IndexModel.StatusColor(o.Status),
            totalRevenue = o.TotalRevenue,
            amountPaid = o.AmountPaid,
            outstanding = o.Outstanding,
            seatTotal = o.SeatTotal,
            tourTitle = o.TourTitle,
            departureDate = o.DepartureDate?.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture),
        }).ToList();

        return new JsonResult(new { draw = dt.Draw, recordsTotal = result.Total, recordsFiltered = result.Total, data });
    }

    /// <summary>Lịch chăm sóc của đúng khách này — cùng lý do phân trang tại server như trên.</summary>
    public async Task<IActionResult> OnGetCaresAsync(Guid id)
    {
        var dt = ParseDataTables();
        var result = await _cares.ListAsync(dt.Page, dt.Size,
            new TourKit.Application.Crm.Dtos.CustomerCareListFilter(Q: dt.Keyword, CustomerId: id));

        var data = result.Items.Select(c => new
        {
            id = c.Id,
            title = c.Title,
            detail = c.Detail,
            feedback = c.Feedback,
            remindAt = c.RemindAt?.ToString("dd/MM/yyyy", System.Globalization.CultureInfo.InvariantCulture),
            assigneeName = c.AssigneeName,
            statusLabel = TrangThaiChamSoc(c.Status),
            statusColor = MauTrangThaiChamSoc(c.Status),
        }).ToList();

        return new JsonResult(new { draw = dt.Draw, recordsTotal = result.Total, recordsFiltered = result.Total, data });
    }

    // Bám đúng bộ trạng thái của màn Chăm sóc khách hàng (0 mới · 1 đang xử lý · 2 hoàn thành).
    private static string TrangThaiChamSoc(int s) => s switch
    {
        0 => "Mới",
        1 => "Đang xử lý",
        2 => "Hoàn thành",
        _ => "Khác",
    };

    private static string MauTrangThaiChamSoc(int s) => s switch
    {
        0 => "info",
        1 => "warning",
        2 => "success",
        _ => "secondary",
    };

    /// <summary>2 chữ cái đầu cho avatar (khi không có ảnh).</summary>
    public static string Initials(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) { return "?"; }
        var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 1
            ? parts[0][..1].ToUpperInvariant()
            : (parts[0][..1] + parts[^1][..1]).ToUpperInvariant();
    }
}
