using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.Providers;
using TourKit.Application.Rooms;
using TourKit.Application.Rooms.Dtos;

namespace TourKit.Api.Pages.RoomAllotments;

// Quỹ phòng / allotment — 2 khung: LƯỚI LỊCH (ma trận NCC/dịch vụ × ngày, đúng dạng bản cũ
// web/src/features/rooms/RoomFundPage.tsx) và BẢNG (DataTables server-side).
// Bản cũ nạp toàn bộ ô quỹ rồi gom ma trận ở client → vi phạm "không get-all". Ở đây lưới giữ nguyên dạng
// nhưng handler Grid CHỈ nạp ô quỹ trong CỬA SỔ NGÀY đang xem (tối đa 31 ngày) và CHỈ trả tối đa `size`
// HÀNG (NCC + dịch vụ) mỗi trang, quét có trần ô — không get-all.
// Giữ đủ thông tin bản cũ: 5 KPI, chú giải loại ngày, 7 tiêu chí lọc (từ khoá · tỉnh thành · thị trường ·
// NCC · hạng sao · khoảng ngày), cột ghép NCC/Dịch vụ, Tồn/Đặt/Còn + giá NET, dòng tổng cộng,
// offcanvas CRUD + xoá.
[Authorize(Policy = "roomfund.view")]
public class IndexModel : TkListPageModel
{
    /// <summary>Cửa sổ ngày tối đa của lưới — vượt thì trả cờ để UI bắt thu hẹp.</summary>
    private const int MaxGridDays = 31;

    /// <summary>Trần số hàng (NCC + dịch vụ) một trang lưới.</summary>
    private const int MaxGridRows = 100;

    /// <summary>Kích thước một lượt quét ô quỹ khi gom hàng.</summary>
    private const int GridChunk = 500;

    /// <summary>Trần tổng số ô quỹ được quét cho một trang lưới (chặn tuyệt đối get-all).</summary>
    private const int MaxGridScanCells = 5000;

    private readonly IRoomAllotmentService _svc;
    private readonly IProviderService _providers;

    public IndexModel(IRoomAllotmentService svc, IProviderService providers)
    {
        _svc = svc;
        _providers = providers;
    }

    public RoomAllotmentStatsDto Stats { get; private set; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0);
    public IReadOnlyList<(Guid Id, string Name)> Providers { get; private set; } = [];

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập mã NCC")] public string ProviderRef { get; set; } = "";
        [Required(ErrorMessage = "Bắt buộc nhập tên dịch vụ/phòng")] public string ServiceName { get; set; } = "";
        public string? ProjectName { get; set; }
        public string? Province { get; set; }
        public string? Market { get; set; }
        [Required(ErrorMessage = "Bắt buộc chọn ngày")] public DateTimeOffset? Date { get; set; }
        public int DayType { get; set; }
        public int Quota { get; set; }
        public int Booked { get; set; }
        public decimal Price { get; set; }
        public int? Rating { get; set; }
        public string? Note { get; set; }
    }

    /// <summary>Chú giải loại ngày (0 thường · 1 cuối tuần · 2 lễ tết · 3 cao điểm).</summary>
    public static readonly (int Value, string Label)[] DayTypes =
    [
        (0, "Thường"), (1, "Cuối tuần"), (2, "Lễ tết"), (3, "Cao điểm"),
    ];

    public static string DayTypeLabel(int t) => t switch
    {
        1 => "Cuối tuần",
        2 => "Lễ tết",
        3 => "Cao điểm",
        _ => "Thường",
    };

    public static string DayTypeColor(int t) => t switch
    {
        1 => "info",
        2 => "danger",
        3 => "warning",
        _ => "secondary",
    };

    /// <summary>Cửa sổ lưới mặc định: 14 ngày kể từ hôm nay (bám pager 7/14/30 ngày hệ cũ).</summary>
    public string GridFrom { get; private set; } = "";
    public string GridTo { get; private set; } = "";

    public async Task OnGetAsync()
    {
        Stats = await _svc.GetStatsAsync();
        Providers = (await _providers.ListAsync(1, 1000)).Items.Select(p => (p.Id, p.Name)).ToList();

        var today = DateOnly.FromDateTime(DateTime.Today);
        GridFrom = today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        GridTo = today.AddDays(13).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    /// <summary>Dựng bộ lọc từ query — đúng các tiêu chí RoomAllotmentListFilter hỗ trợ.
    /// windowFrom/windowTo (cửa sổ lưới đang xem) nếu có sẽ ĐÈ khoảng ngày của thanh lọc.</summary>
    private RoomAllotmentListFilter BuildFilter(string? keyword, DateTimeOffset? windowFrom = null, DateTimeOffset? windowTo = null)
    {
        var q = Request.Query;
        int? I(string k) => int.TryParse(q[k], out var n) ? n : null;
        string? S(string k) => string.IsNullOrWhiteSpace(q[k]) ? null : q[k].ToString();
        DateTimeOffset? D(string k) => DateTimeOffset.TryParse(q[k], CultureInfo.InvariantCulture, out var d) ? d.ToUniversalTime() : null;

        return new RoomAllotmentListFilter(
            Q: keyword,
            ProjectName: S("projectName"),
            Province: S("province"),
            Market: S("market"),
            ProviderRef: S("providerRef"),
            Rating: I("rating"),
            DateFrom: windowFrom?.ToUniversalTime() ?? D("dateFrom"),
            DateTo: windowTo?.ToUniversalTime() ?? D("dateTo"));
    }

    /// <summary>Nguồn DataTables server-side: chỉ trả đúng 1 trang + KPI/tổng cộng theo bộ lọc đang áp.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var filter = BuildFilter(dt.Keyword);
        var result = await _svc.ListAsync(dt.Page, dt.Size, filter);
        var stats = await _svc.GetStatsAsync(filter);

        var data = result.Items.Select(a => new
        {
            id = a.Id,
            providerRef = a.ProviderRef,
            providerName = string.IsNullOrWhiteSpace(a.ProviderName) ? a.ProviderRef : a.ProviderName,
            serviceName = a.ServiceName,
            projectName = a.ProjectName,
            province = a.Province,
            market = a.Market,
            date = a.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            dateText = a.Date.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            dayType = a.DayType,
            dayTypeLabel = DayTypeLabel(a.DayType),
            dayTypeColor = DayTypeColor(a.DayType),
            quota = a.Quota,
            booked = a.Booked,
            available = a.Available,
            price = a.Price,
            rating = a.Rating,
            note = a.Note,
        }).ToList();

        return new JsonResult(new
        {
            draw = dt.Draw,
            recordsTotal = stats.Cells,
            recordsFiltered = result.Total,
            data,
            stats = new
            {
                cells = stats.Cells,
                providers = stats.Providers,
                totalQuota = stats.TotalQuota,
                totalBooked = stats.TotalBooked,
                totalAvailable = stats.TotalAvailable,
            },
            pageSum = new { price = data.Sum(x => x.price) },
        });
    }

    /// <summary>
    /// Nguồn LƯỚI LỊCH (ma trận NCC/dịch vụ × ngày). Ràng buộc chống get-all:
    /// (1) cửa sổ ngày tối đa <see cref="MaxGridDays"/> — vượt thì trả cờ tooWide và KHÔNG truy vấn;
    /// (2) mỗi trang chỉ trả tối đa <paramref name="size"/> HÀNG (mặc định 25, trần <see cref="MaxGridRows"/>);
    /// (3) quét ô quỹ theo lô <see cref="GridChunk"/> và dừng ngay khi đủ hàng cho trang, trần tuyệt đối
    /// <see cref="MaxGridScanCells"/> ô (vượt thì trả cờ capped).
    /// Gom (ProviderRef + ServiceName) ngay tại server — client chỉ vẽ ma trận đã dựng sẵn.
    /// </summary>
    public async Task<IActionResult> OnGetGridAsync(DateOnly? from, DateOnly? to, int page = 1, int size = 25)
    {
        var start = from ?? DateOnly.FromDateTime(DateTime.Today);
        var end = to ?? start.AddDays(13);
        if (end < start)
        {
            end = start;
        }

        var dayCount = end.DayNumber - start.DayNumber + 1;
        if (dayCount > MaxGridDays)
        {
            return new JsonResult(new { tooWide = true, maxDays = MaxGridDays, days = Array.Empty<object>(), rows = Array.Empty<object>() });
        }

        if (page < 1)
        {
            page = 1;
        }

        size = Math.Clamp(size, 1, MaxGridRows);

        var keyword = string.IsNullOrWhiteSpace(Request.Query["q"]) ? null : Request.Query["q"].ToString();
        var windowFrom = new DateTimeOffset(start.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).ToUniversalTime();
        var windowTo = new DateTimeOffset(end.ToDateTime(TimeOnly.MaxValue), TimeSpan.Zero).ToUniversalTime();
        var filter = BuildFilter(keyword, windowFrom, windowTo);

        // Quét theo lô: service sắp sẵn (ProviderRef, ServiceName, Date) → các ô cùng một hàng nằm liền nhau.
        // Thấy hàng thứ (skip+size+1) xuất hiện ⇒ các hàng 1..skip+size đã trọn vẹn → dừng quét.
        var skip = (page - 1) * size;
        var need = skip + size;
        var order = new List<(string ProviderRef, string ServiceName)>();
        var groups = new Dictionary<(string ProviderRef, string ServiceName), List<RoomAllotmentDto>>();
        var scanned = 0;
        var totalCells = 0;
        var complete = false;
        var capped = false;

        var maxChunks = (MaxGridScanCells / GridChunk) + 1;
        for (var p = 1; p <= maxChunks; p++)
        {
            var chunk = await _svc.ListAsync(p, GridChunk, filter);
            totalCells = chunk.Total;

            foreach (var a in chunk.Items)
            {
                var key = (a.ProviderRef, a.ServiceName);
                if (!groups.TryGetValue(key, out var list))
                {
                    list = [];
                    groups[key] = list;
                    order.Add(key);
                }

                list.Add(a);
            }

            scanned += chunk.Items.Count;
            if (chunk.Items.Count < GridChunk || scanned >= chunk.Total)
            {
                complete = true;
                break;
            }

            if (order.Count > need)
            {
                break;
            }

            if (scanned >= MaxGridScanCells)
            {
                capped = true;
                break;
            }
        }

        // Cột ngày của cửa sổ đang xem.
        var dayKeys = new List<string>(dayCount);
        var days = new List<object>(dayCount);
        for (var i = 0; i < dayCount; i++)
        {
            var d = start.AddDays(i);
            var key = d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            dayKeys.Add(key);
            days.Add(new
            {
                date = key,
                dayText = d.ToString("dd/MM", CultureInfo.InvariantCulture),
                weekday = WeekdayShort(d.DayOfWeek),
                isWeekend = d.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday,
            });
        }

        var rows = order.Skip(skip).Take(size).Select(k =>
        {
            var cells = groups[k];
            var first = cells[0];
            var byDate = cells
                .GroupBy(c => c.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                .ToDictionary(g => g.Key, g => (IReadOnlyList<RoomAllotmentDto>)g.ToList());

            return new
            {
                providerRef = k.ProviderRef,
                providerName = string.IsNullOrWhiteSpace(first.ProviderName) ? k.ProviderRef : first.ProviderName,
                serviceName = k.ServiceName,
                projectName = first.ProjectName,
                province = first.Province,
                market = first.Market,
                rating = first.Rating,
                quota = cells.Sum(c => c.Quota),
                booked = cells.Sum(c => c.Booked),
                cells = dayKeys.Select(dk => BuildGridCell(byDate.GetValueOrDefault(dk))).ToList(),
            };
        }).ToList();

        var stats = await _svc.GetStatsAsync(filter);

        return new JsonResult(new
        {
            tooWide = false,
            from = start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            to = end.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            days,
            rows,
            page,
            size,
            // rowsTotal chỉ CHÍNH XÁC khi đã quét hết cửa sổ; nếu không, UI dùng hasMore để bật nút ›.
            rowsTotal = complete ? order.Count : (int?)null,
            hasMore = order.Count > skip + rows.Count,
            capped,
            scannedCells = scanned,
            totalCells,
            stats = new
            {
                cells = stats.Cells,
                providers = stats.Providers,
                totalQuota = stats.TotalQuota,
                totalBooked = stats.TotalBooked,
                totalAvailable = stats.TotalAvailable,
            },
        });
    }

    /// <summary>Một ô ma trận = các bản ghi quỹ của (NCC + dịch vụ) trong đúng 1 ngày; null nếu không có.</summary>
    private static object? BuildGridCell(IReadOnlyList<RoomAllotmentDto>? list)
    {
        if (list is null || list.Count == 0)
        {
            return null;
        }

        var top = list[0];
        var quota = list.Sum(c => c.Quota);
        var booked = list.Sum(c => c.Booked);

        return new
        {
            id = top.Id,
            quota,
            booked,
            available = Math.Max(0, quota - booked),
            price = top.Price,
            dayType = top.DayType,
            dayTypeLabel = DayTypeLabel(top.DayType),
            dayTypeColor = DayTypeColor(top.DayType),
            count = list.Count,
            note = top.Note,
        };
    }

    private static string WeekdayShort(DayOfWeek d) => d switch
    {
        DayOfWeek.Monday => "T2",
        DayOfWeek.Tuesday => "T3",
        DayOfWeek.Wednesday => "T4",
        DayOfWeek.Thursday => "T5",
        DayOfWeek.Friday => "T6",
        DayOfWeek.Saturday => "T7",
        _ => "CN",
    };

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid || Input.Date is not DateTimeOffset date)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        try
        {
            if (Id is Guid g && g != Guid.Empty)
            {
                await _svc.UpdateAsync(g, new UpdateRoomAllotmentDto(
                    Input.ProviderRef, Input.ServiceName, Input.ProjectName, Input.Province, Input.Market,
                    date.ToUniversalTime(), Input.DayType, Input.Quota, Input.Booked, Input.Price, Input.Rating, Input.Note));
            }
            else
            {
                await _svc.CreateAsync(new CreateRoomAllotmentDto(
                    Input.ProviderRef, Input.ServiceName, Input.ProjectName, Input.Province, Input.Market,
                    date.ToUniversalTime(), Input.DayType, Input.Quota, Input.Booked, Input.Price, Input.Rating, Input.Note));
            }
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }

        return new JsonResult(Result.Success("Đã lưu quỹ phòng."));
    }

    /// <summary>Xoá ô quỹ — trả Result để bảng server-side reload tại chỗ.</summary>
    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _svc.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }

        return new JsonResult(Result.Success("Đã xoá ô quỹ phòng."));
    }
}
