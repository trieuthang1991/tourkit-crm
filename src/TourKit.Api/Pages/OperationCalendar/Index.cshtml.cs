using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Application.Admin;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;

namespace TourKit.Api.Pages.OperationCalendar;

// Lịch điều hành — DataTables SERVER-SIDE (không get-all).
// Bản cũ (web/src/features/operations/OperationsCalendarPage.tsx → booking/DepartureCalendar.tsx) là LỊCH
// THÁNG: nạp 500 chuyến rồi gom theo ngày ở client → vi phạm "không get-all". Ở đây thay bằng BẢNG
// server-side sắp theo ngày khởi hành + lọc khoảng ngày (mặc định = tháng hiện tại), giữ đủ thông tin
// ô lịch cũ (ngày · tên/mã chuyến · số chỗ · tổng "N chuyến · X chỗ" ở dòng tổng cộng).
[Authorize(Policy = "departure.view")]
public class IndexModel : TkListPageModel
{
    private readonly IDepartureService _svc;
    private readonly IUserAdminService _users;

    public IndexModel(IDepartureService svc, IUserAdminService users)
    {
        _svc = svc;
        _users = users;
    }

    public DepartureStatsDto Stats { get; private set; } = new(0, 0, 0, 0);
    public IReadOnlyList<(Guid Id, string Name)> Users { get; private set; } = [];
    public IReadOnlyList<string> TourTypes { get; private set; } = [];

    /// <summary>Mặc định khoảng ngày = tháng hiện tại (bám lịch tháng hệ cũ).</summary>
    public string MonthFrom { get; private set; } = "";
    public string MonthTo { get; private set; } = "";

    public static string StatusLabel(bool isClosed) => isClosed ? "Đã đóng" : "Đang mở";
    public static string StatusColor(bool isClosed) => isClosed ? "secondary" : "success";

    public async Task OnGetAsync()
    {
        Stats = await _svc.GetStatsAsync();
        TourTypes = (await _svc.GetFilterOptionsAsync()).TourTypes;
        Users = (await _users.ListAsync()).Select(u => (u.Id, u.FullName)).ToList();

        var today = DateTimeOffset.Now;
        var first = new DateTimeOffset(today.Year, today.Month, 1, 0, 0, 0, today.Offset);
        MonthFrom = first.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        MonthTo = first.AddMonths(1).AddDays(-1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    /// <summary>Dựng bộ lọc từ query — đúng các tiêu chí DepartureListFilter hỗ trợ.</summary>
    private DepartureListFilter BuildFilter(string? keyword)
    {
        var q = Request.Query;
        Guid? G(string k) => Guid.TryParse(q[k], out var g) ? g : null;
        bool? B(string k) => bool.TryParse(q[k], out var b) ? b : null;
        string? S(string k) => string.IsNullOrWhiteSpace(q[k]) ? null : q[k].ToString();
        DateTimeOffset? D(string k) => DateTimeOffset.TryParse(q[k], CultureInfo.InvariantCulture, out var d) ? d.ToUniversalTime() : null;

        return new DepartureListFilter(
            Q: keyword,
            TourType: S("tourType"),
            Status: null,
            AssignedToUserId: G("assignedToUserId"),
            IsClosed: B("isClosed"),
            DepartureFrom: D("departureFrom"),
            DepartureTo: D("departureTo"));
    }

    /// <summary>Nguồn DataTables server-side: chỉ trả đúng 1 trang.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var result = await _svc.ListAsync(dt.Page, dt.Size, BuildFilter(dt.Keyword));
        var stats = await _svc.GetStatsAsync();

        // Tên NV điều hành: chỉ tra cho user xuất hiện trong TRANG hiện tại.
        var userIds = result.Items.Where(d => d.AssignedToUserId is not null).Select(d => d.AssignedToUserId!.Value).ToHashSet();
        var names = userIds.Count == 0
            ? []
            : (await _users.ListAsync()).Where(u => userIds.Contains(u.Id)).ToDictionary(u => u.Id, u => u.FullName);

        // Sắp theo ngày khởi hành TĂNG DẦN trong trang (lịch điều hành nhìn theo trình tự ngày).
        var items = result.Items
            .OrderBy(d => d.DepartureDate ?? DateTimeOffset.MaxValue)
            .Select(d => new
            {
                id = d.Id,
                code = d.Code,
                title = d.Title,
                tourType = d.TourType ?? "—",
                departureDateText = d.DepartureDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "—",
                weekdayText = d.DepartureDate is DateTimeOffset dd ? WeekdayVi(dd) : "",
                endDateText = d.EndDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                totalSlots = d.TotalSlots,
                assigneeName = d.AssignedToUserId is Guid g && names.TryGetValue(g, out var n) ? n : "—",
                isClosed = d.IsClosed,
                statusLabel = StatusLabel(d.IsClosed),
                statusColor = StatusColor(d.IsClosed),
            })
            .ToList();

        // Tổng cộng TRANG HIỆN TẠI — bám nhãn "N chuyến · X chỗ" trên ô lịch hệ cũ.
        var pageSum = new { count = items.Count, slots = items.Sum(x => x.totalSlots) };

        return new JsonResult(new
        {
            draw = dt.Draw,
            recordsTotal = stats.Total,
            recordsFiltered = result.Total,
            data = items,
            pageSum,
            // Khối stats ngoài contract DataTables → KPI tự làm tươi qua sự kiện xhr.dt (không thêm roundtrip).
            stats = new { total = stats.Total, upcoming = stats.Upcoming, closed = stats.Closed, totalSlots = stats.TotalSlots },
        });
    }

    private static string WeekdayVi(DateTimeOffset d) => d.DayOfWeek switch
    {
        DayOfWeek.Monday => "Thứ 2",
        DayOfWeek.Tuesday => "Thứ 3",
        DayOfWeek.Wednesday => "Thứ 4",
        DayOfWeek.Thursday => "Thứ 5",
        DayOfWeek.Friday => "Thứ 6",
        DayOfWeek.Saturday => "Thứ 7",
        _ => "Chủ nhật",
    };
}
