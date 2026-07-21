using TourKit.Api.Services;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Catalog;
using TourKit.Shared.Enums;

namespace TourKit.Api.Pages.Orders;

// Danh sách đơn: DataTables SERVER-SIDE + GIỮ ĐỦ thông tin của bản cũ
// (web/src/features/booking/OrdersPage.tsx): 6 KPI, 17 bộ lọc, cột kép Thu/Chi,
// SeatTags, lợi nhuận, dòng tổng cộng trang, export CSV.
[Authorize(Policy = "booking.view")]
public class IndexModel : TkListPageModel
{
    private readonly IBookingService _svc;
    private readonly UserDirectory _users;
    private readonly IBranchService _branches;
    private readonly IDepartmentService _departments;
    private readonly IMarketTypeService _markets;
    private readonly ITourGroupService _groups;

    public IndexModel(IBookingService svc, UserDirectory users, IBranchService branches,
        IDepartmentService departments, IMarketTypeService markets, ITourGroupService groups)
    {
        _svc = svc;
        _users = users;
        _branches = branches;
        _departments = departments;
        _markets = markets;
        _groups = groups;
    }

    public OrderStatsDto Stats { get; private set; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    public OrderFilterOptionsDto Options { get; private set; } = new([], [], []);
    public IReadOnlyList<(Guid Id, string Name)> Users { get; private set; } = [];
    public IReadOnlyList<(Guid Id, string Name)> Branches { get; private set; } = [];
    public IReadOnlyList<(Guid Id, string Name)> Departments { get; private set; } = [];
    public IReadOnlyList<(Guid Id, string Name)> Markets { get; private set; } = [];
    public IReadOnlyList<(Guid Id, string Name)> Groups { get; private set; } = [];

    [BindProperty(SupportsGet = true, Name = "bookingType")] public int? BookingType { get; set; }

    /// <summary>
    /// Loại đơn hiệu lực: ưu tiên đoạn "loai" trong route mới (/don-hang/loai/tour-fit) rồi mới tới
    /// query cũ (?bookingType=0). Route giữ được khi DataTables gọi ?handler=Data trên cùng URL.
    /// </summary>
    private int? EffectiveBookingType
    {
        get
        {
            if (RouteData.Values.TryGetValue("loai", out var raw) && raw is string slug
                && TourKit.Api.Routing.RouteMap.OrderLoai.TryGetValue(slug, out var t))
            {
                return t;
            }
            return BookingType;
        }
    }

    // Loại tour (BookingType): 0 FIT · 1 GIT · 2 LandTour/Combo · 3 Booking phòng · 4 Dịch vụ lẻ · 5 Visa · 6 Xe.
    public static readonly string[] BookingTypeLabels = ["Tour FIT", "Tour GIT/Combo", "LandTour", "Booking phòng", "Dịch vụ lẻ", "Visa", "Xe"];
    public string TypeLabel => EffectiveBookingType is int t && t >= 0 && t < BookingTypeLabels.Length ? BookingTypeLabels[t] : "Tất cả";

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
        OrderStatus.Confirmed => "success",
        OrderStatus.Cancelled => "danger",
        OrderStatus.Closed => "info",
        _ => "secondary",
    };

    public async Task OnGetAsync()
    {
        Stats = await _svc.GetOrderStatsAsync();
        Options = await _svc.GetOrderFilterOptionsAsync();
        Users = (await _users.ListAsync()).Select(u => (u.Id, u.FullName)).ToList();
        Branches = (await _branches.ListAsync()).Select(b => (b.Id, b.Name)).ToList();
        Departments = (await _departments.ListAsync()).Select(d => (d.Id, d.Name)).ToList();
        Markets = (await _markets.ListAsync()).Select(m => (m.Id, m.Name)).ToList();
        Groups = (await _groups.ListAsync()).Select(g => (g.Id, g.Name)).ToList();
    }

    /// <summary>Dựng bộ lọc từ query — giữ đủ 17 tiêu chí của thanh lọc hệ cũ.</summary>
    private OrderListFilter BuildFilter(string? keyword)
    {
        var q = Request.Query;
        int? I(string k) => int.TryParse(q[k], out var n) ? n : null;
        Guid? G(string k) => Guid.TryParse(q[k], out var g) ? g : null;
        DateTimeOffset? D(string k) => DateTimeOffset.TryParse(q[k], CultureInfo.InvariantCulture, out var d) ? d.ToUniversalTime() : null;
        string? S(string k) => string.IsNullOrWhiteSpace(q[k]) ? null : q[k].ToString();
        bool? B(string k) => bool.TryParse(q[k], out var b) ? b : null;

        return new OrderListFilter(
            Q: keyword,
            Status: I("status"),
            PaymentStatus: I("paymentStatus"),
            DepartureFrom: D("departureFrom"), DepartureTo: D("departureTo"),
            CreatedFrom: D("createdFrom"), CreatedTo: D("createdTo"),
            SalesUserId: G("salesUserId"), BranchId: G("branchId"),
            CreatedByUserId: G("createdByUserId"), DepartmentId: G("departmentId"),
            TourType: S("tourType"), ProviderId: G("providerId"),
            MarketTypeId: G("marketTypeId"), TourGroupId: G("tourGroupId"),
            BookingType: I("bookingType") ?? EffectiveBookingType,
            CommissionSettled: B("commissionSettled"),
            OperationalStatus: I("operationalStatus"),
            CollaboratorId: G("collaboratorId"),
            InvoiceStatus: I("invoiceStatus"));
    }

    /// <summary>Nguồn DataTables server-side: chỉ trả đúng 1 trang.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var result = await _svc.ListOrdersAsync(dt.Page, dt.Size, BuildFilter(dt.Keyword));

        // Tên sales: chỉ tra cho user xuất hiện trong TRANG hiện tại.
        var salesIds = result.Items.Where(o => o.SalesUserId is not null).Select(o => o.SalesUserId!.Value).ToHashSet();
        var names = salesIds.Count == 0
            ? []
            : (await _users.ListAsync()).Where(u => salesIds.Contains(u.Id)).ToDictionary(u => u.Id, u => u.FullName);

        var stats = await _svc.GetOrderStatsAsync();
        var items = result.Items.Select(o => new
        {
            id = o.Id,
            code = o.Code,
            customerName = o.CustomerName ?? "—",
            tourTitle = o.TourTitle ?? "—",
            seatTotal = o.SeatTotal,
            seatHeld = o.SeatHeld,
            seatSold = o.SeatSold,
            seatRemaining = o.SeatRemaining,
            departureDate = o.DepartureDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "—",
            totalRevenue = o.TotalRevenue,
            amountPaid = o.AmountPaid,
            outstanding = o.Outstanding,
            totalCost = o.TotalCost,
            actualCost = o.ActualCost,
            profit = o.TotalRevenue - o.TotalCost,
            salesName = o.SalesUserId is Guid g && names.TryGetValue(g, out var n) ? n : "—",
            statusLabel = StatusLabel(o.Status),
            statusColor = StatusColor(o.Status),
        }).ToList();

        // Tổng cộng TRANG HIỆN TẠI (bám dòng tfoot hệ cũ — 6 chỉ số).
        var pageSum = new
        {
            revenue = items.Sum(x => x.totalRevenue),
            paid = items.Sum(x => x.amountPaid),
            cost = items.Sum(x => x.totalCost),
            actualCost = items.Sum(x => x.actualCost),
            profit = items.Sum(x => x.profit),
            outstanding = items.Sum(x => x.outstanding),
        };

        return new JsonResult(new
        {
            draw = dt.Draw,
            recordsTotal = stats.Total,
            recordsFiltered = result.Total,
            data = items,
            pageSum,
        });
    }

    /// <summary>Xuất CSV theo đúng bộ lọc đang áp (giới hạn 5000 dòng để không sập).</summary>
    public async Task<IActionResult> OnGetExportAsync()
    {
        const int max = 5000;
        var result = await _svc.ListOrdersAsync(1, max, BuildFilter(Request.Query["search"].ToString() is { Length: > 0 } s ? s : null));

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Mã đơn,Khách hàng,Tour,Ngày đi,Tổng thu,Thực thu,Còn nợ,Tổng chi,Thực chi,Lợi nhuận,Trạng thái");
        foreach (var o in result.Items)
        {
            string C(string? v) => "\"" + (v ?? "").Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
            sb.Append(C(o.Code)).Append(',').Append(C(o.CustomerName)).Append(',').Append(C(o.TourTitle)).Append(',')
              .Append(C(o.DepartureDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture))).Append(',')
              .Append(o.TotalRevenue.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(o.AmountPaid.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(o.Outstanding.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(o.TotalCost.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(o.ActualCost.ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append((o.TotalRevenue - o.TotalCost).ToString(CultureInfo.InvariantCulture)).Append(',')
              .Append(C(StatusLabel(o.Status))).AppendLine();
        }

        var bytes = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return File(bytes, "text/csv", "don-hang.csv");
    }
}
