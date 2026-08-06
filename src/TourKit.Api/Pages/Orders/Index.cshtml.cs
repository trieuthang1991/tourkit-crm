using TourKit.Api.Services;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Auth;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.Booking;
using TourKit.Application.Common;
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
    private readonly ICustomerSourceService _sources;
    private readonly TourKit.Application.Finance.IReceiptService _receipts;
    private readonly TourKit.Application.Finance.IPaymentService _payments;
    private readonly ICurrentUser _current;

    public IndexModel(IBookingService svc, UserDirectory users, IBranchService branches,
        IDepartmentService departments, IMarketTypeService markets, ITourGroupService groups,
        ICustomerSourceService sources,
        TourKit.Application.Finance.IReceiptService receipts, TourKit.Application.Finance.IPaymentService payments,
        ICurrentUser current)
    {
        _svc = svc;
        _users = users;
        _branches = branches;
        _departments = departments;
        _markets = markets;
        _groups = groups;
        _sources = sources;
        _receipts = receipts;
        _payments = payments;
        _current = current;
    }

    // ---- Action ngay trên DÒNG danh sách (bám hệ cũ: thao tác ở list, không sang trang) — trả JSON cho AJAX ----
    public async Task<IActionResult> OnPostConfirmAsync(Guid id)
    {
        try { await _svc.ConfirmOrderAsync(id); return new JsonResult(Result.Success("Đã xác nhận đơn.")); }
        catch (AppException ex) { return new JsonResult(Result.Error(ex.Message)); }
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid id)
    {
        try { await _svc.CancelOrderAsync(id); return new JsonResult(Result.Success("Đã huỷ đơn.")); }
        catch (AppException ex) { return new JsonResult(Result.Error(ex.Message)); }
    }

    public async Task<IActionResult> OnPostCloseAsync(Guid id)
    {
        if (_current.UserId is not Guid uid) { return new JsonResult(Result.Error("Không xác định được người dùng hiện tại.")); }
        try { await _svc.CloseOrderAsync(id, uid); return new JsonResult(Result.Success("Đã tất toán đơn.")); }
        catch (AppException ex) { return new JsonResult(Result.Error(ex.Message)); }
    }

    public async Task<IActionResult> OnPostReopenAsync(Guid id)
    {
        try { await _svc.ReopenOrderAsync(id); return new JsonResult(Result.Success("Đã mở lại đơn.")); }
        catch (AppException ex) { return new JsonResult(Result.Error(ex.Message)); }
    }

    /// <summary>Đổi tình trạng vận hành đơn tour ngay trên dòng (bám staging cột "Trạng thái").</summary>
    public async Task<IActionResult> OnPostOpStatusAsync(Guid id, int status)
    {
        try { await _svc.SetOperationalStatusAsync(id, status); return new JsonResult(Result.Success("Đã đổi trạng thái.")); }
        catch (AppException ex) { return new JsonResult(Result.Error(ex.Message)); }
    }

    /// <summary>Đổi trạng thái quy trình visa ngay trên dòng (11 bước staging).</summary>
    public async Task<IActionResult> OnPostVisaStatusAsync(Guid id, int status)
    {
        try { await _svc.SetVisaStatusAsync(id, status); return new JsonResult(Result.Success("Đã đổi trạng thái visa.")); }
        catch (AppException ex) { return new JsonResult(Result.Error(ex.Message)); }
    }

    public async Task<IActionResult> OnPostReceiptAsync(Guid id, decimal amount, string? method, string? note)
    {
        try
        {
            await _receipts.CreateAsync(id, new TourKit.Application.Finance.Dtos.CreateReceiptDto(
                amount, string.IsNullOrWhiteSpace(method) ? "cash" : method, null, note));
            return new JsonResult(Result.Success("Đã tạo phiếu thu."));
        }
        catch (AppException ex) { return new JsonResult(Result.Error(ex.Message)); }
    }

    public async Task<IActionResult> OnPostPaymentAsync(Guid id, decimal amount, string? method, string? receiver, string? note)
    {
        try
        {
            await _payments.CreateAsync(id, new TourKit.Application.Finance.Dtos.CreatePaymentDto(
                null, null, amount, string.IsNullOrWhiteSpace(method) ? "cash" : method, null, receiver, note));
            return new JsonResult(Result.Success("Đã tạo phiếu chi."));
        }
        catch (AppException ex) { return new JsonResult(Result.Error(ex.Message)); }
    }

    /// <summary>Số dư đơn (cho popup xem nhanh + mặc định phiếu thu = còn nợ).</summary>
    public async Task<IActionResult> OnGetBalanceAsync(Guid id)
    {
        var b = await _receipts.GetBalanceAsync(id);
        return new JsonResult(new { total = b.Total, paid = b.Paid, outstanding = b.Outstanding });
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

    /// <summary>Loại KHÔNG phải tour (Dịch vụ lẻ 4 · Visa 5 · Xe 6): ẩn cột đặc thù tour (Khởi hành, Chỗ/pax)
    /// vì không có ý nghĩa — bám staging (màn Visa/Dịch vụ không có cột Tour/Khởi hành/Chỗ).</summary>
    public bool HideTourColumns => EffectiveBookingType is 4 or 5 or 6;

    /// <summary>Loại đơn Visa (5): hiện các cột quy trình visa.</summary>
    public bool IsVisa => EffectiveBookingType is 5;

    // Trạng thái Visa — 11 bước bám staging (VisaStatus 0..10).
    public static readonly string[] VisaStatusLabels =
        ["Tạo mới", "Đã tiếp nhận", "Đang xử lý", "Đã nộp", "Yêu cầu bổ sung", "Chờ kết quả", "Đậu", "Rớt", "Rút hồ sơ", "Hủy hồ sơ", "Hoàn tất dịch vụ"];
    public static string VisaStatusLabel(int? s) => s is int v && v >= 0 && v < VisaStatusLabels.Length ? VisaStatusLabels[v] : VisaStatusLabels[0];
    public static string VisaStatusColor(int? s) => s switch
    {
        6 => "success", 10 => "success",              // Đậu · Hoàn tất
        7 => "danger", 9 => "danger",                 // Rớt · Hủy hồ sơ
        4 => "warning", 8 => "warning",               // Yêu cầu bổ sung · Rút hồ sơ
        2 or 3 => "primary", 1 or 5 => "info",        // Đang xử lý/Đã nộp · Đã tiếp nhận/Chờ kết quả
        _ => "secondary",                             // Tạo mới
    };

    // Tình trạng vận hành (OrderOperationalStatus) — cột "Trạng thái" cho đơn TOUR (bám staging).
    public static string OpStatusLabel(int s) => (OrderOperationalStatus)s switch
    {
        OrderOperationalStatus.Upcoming => "Sắp chạy",
        OrderOperationalStatus.PriceActivated => "Kích hoạt giá",
        OrderOperationalStatus.Running => "Đang chạy",
        OrderOperationalStatus.PendingSettlement => "Chưa quyết toán",
        OrderOperationalStatus.Settled => "Đã quyết toán",
        OrderOperationalStatus.Done => "Hoàn thành",
        OrderOperationalStatus.Cancelled => "Hủy",
        OrderOperationalStatus.CancelledNoShow => "Hủy không đi",
        _ => "—",
    };
    public static string OpStatusColor(int s) => (OrderOperationalStatus)s switch
    {
        OrderOperationalStatus.Settled or OrderOperationalStatus.Done => "success",
        OrderOperationalStatus.Cancelled or OrderOperationalStatus.CancelledNoShow => "danger",
        OrderOperationalStatus.PendingSettlement => "warning",
        OrderOperationalStatus.Running => "primary",
        OrderOperationalStatus.PriceActivated => "info",
        _ => "secondary",
    };

    /// <summary>Danh sách trạng thái vận hành cho dropdown theo LOẠI đơn (bám đúng bộ staging từng loại).</summary>
    public int[] OpStatusOptions => EffectiveBookingType switch
    {
        1 => [1, 2, 3, 4, 5, 6, 7],       // GIT: Sắp chạy·Đang chạy·Chưa QT·Đã QT·Hoàn thành·Hủy·Hủy không đi
        _ => [1, 8, 2, 3, 4, 5, 6],       // FIT/LandTour: Sắp chạy·Kích hoạt giá·Đang chạy·Chưa QT·Đã QT·Hoàn thành·Hủy
    };

    /// <summary>Loại đơn TOUR (FIT/GIT/LandTour) — cột "Trạng thái" dùng dropdown tình trạng vận hành.</summary>
    public bool IsTour => EffectiveBookingType is null or 0 or 1 or 2;

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
        Sources = (await _sources.ListAsync()).Select(s => s.Name).ToList();
    }

    public IReadOnlyList<string> Sources { get; private set; } = [];

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
            InvoiceStatus: I("invoiceStatus"),
            VisaStatus: I("visaStatus"),
            CustomerType: I("customerType"), CustomerSource: S("customerSource"));
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
            status = (int)o.Status,
            statusLabel = StatusLabel(o.Status),
            statusColor = StatusColor(o.Status),
            // Tình trạng vận hành (đơn tour) — hiện trên cột "Trạng thái" dạng dropdown đổi trên dòng.
            opStatus = o.OperationalStatus,
            opStatusLabel = OpStatusLabel(o.OperationalStatus),
            opStatusColor = OpStatusColor(o.OperationalStatus),
            visaStatus = o.VisaStatus ?? 0,
            bookingType = o.BookingType,
            // Quy trình Visa (chỉ hiện ở màn Visa): ngày nhận/nộp/trả + trạng thái + TG xét duyệt (tính toán).
            visaReceiveDate = o.VisaReceiveDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "—",
            visaSubmitDate = o.VisaSubmitDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "—",
            visaReturnDate = o.VisaReturnDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "—",
            visaReviewDays = o.VisaSubmitDate is { } vs && o.VisaReturnDate is { } vr ? (int?)(vr - vs).Days : null,
            visaStatusLabel = VisaStatusLabel(o.VisaStatus),
            visaStatusColor = VisaStatusColor(o.VisaStatus),
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

        return GridJson(dt, stats.Total, result.Total, items,
            new Dictionary<string, object?> { ["pageSum"] = pageSum });
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
