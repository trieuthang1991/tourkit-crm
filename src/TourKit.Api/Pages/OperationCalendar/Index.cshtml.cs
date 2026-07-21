using TourKit.Api.Services;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;

namespace TourKit.Api.Pages.OperationCalendar;

// Lịch điều hành — 2 khung: LỊCH (FullCalendar của template) và BẢNG (DataTables server-side).
// Bản cũ nạp 500 chuyến rồi gom theo ngày ở client. Ở đây lịch dùng handler Events chỉ nạp chuyến
// TRONG KHOẢNG ĐANG XEM (FullCalendar gửi start/end mỗi lần đổi tháng) → không get-all mà vẫn giữ
// đúng dạng lịch; khung bảng giữ nguyên phân trang server + lọc.
[Authorize(Policy = "departure.view")]
public class IndexModel : TkListPageModel
{
    private readonly IDepartureService _svc;
    private readonly UserDirectory _users;

    public IndexModel(IDepartureService svc, UserDirectory users)
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

    /// <summary>
    /// Nguồn sự kiện cho FullCalendar: CHỈ nạp chuyến nằm trong khoảng lịch đang xem
    /// (FullCalendar gửi start/end của khung hiện tại) — không get-all. Trần 500 sự kiện/khung
    /// để một tháng bất thường cũng không kéo sập trình duyệt.
    /// </summary>
    public async Task<IActionResult> OnGetEventsAsync(DateTimeOffset? start, DateTimeOffset? end)
    {
        const int max = 500;
        var q = Request.Query;
        Guid? G(string k) => Guid.TryParse(q[k], out var g) ? g : null;
        bool? B(string k) => bool.TryParse(q[k], out var b) ? b : null;
        string? S(string k) => string.IsNullOrWhiteSpace(q[k]) ? null : q[k].ToString();

        var filter = new DepartureListFilter(
            Q: S("q"),
            TourType: S("tourType"),
            Status: null,
            AssignedToUserId: G("assignedToUserId"),
            IsClosed: B("isClosed"),
            DepartureFrom: start?.ToUniversalTime(),
            DepartureTo: end?.ToUniversalTime());

        var result = await _svc.ListAsync(1, max, filter);
        var events = result.Items
            .Where(d => d.DepartureDate is not null)
            .Select(d => new
            {
                id = d.Id,
                title = $"{d.Code} — {d.Title}",
                start = d.DepartureDate!.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                // FullCalendar coi 'end' là mốc loại trừ → +1 ngày để thanh phủ hết ngày về.
                end = d.EndDate?.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                className = d.IsClosed ? "bg-label-secondary" : "bg-label-success",
                extendedProps = new
                {
                    code = d.Code,
                    tourType = d.TourType ?? "—",
                    totalSlots = d.TotalSlots,
                    statusLabel = StatusLabel(d.IsClosed),
                },
            })
            .ToList();

        return new JsonResult(new { events, truncated = result.Total > max, total = result.Total });
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
