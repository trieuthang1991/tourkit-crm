using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.Admin;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Catalog;

namespace TourKit.Api.Pages.Departures;

// Chuyến đi / LKH: DataTables SERVER-SIDE + GIỮ ĐỦ thông tin bản cũ
// (web/src/features/booking/DeparturesPage.tsx): 4 KPI, tab Tất cả/Đang mở/Đã đóng,
// 4 tiêu chí lọc (từ khoá · loại tour · NV điều hành · khoảng ngày khởi hành),
// cột ghép Tour/LKH + Ngày đi/về + Điều hành, offcanvas tạo chuyến, nút Đóng chuyến.
[Authorize(Policy = "departure.view")]
public class IndexModel : TkListPageModel
{
    private readonly IDepartureService _svc;
    private readonly ITourTemplateService _templates;
    private readonly IUserAdminService _users;

    public IndexModel(IDepartureService svc, ITourTemplateService templates, IUserAdminService users)
    {
        _svc = svc;
        _templates = templates;
        _users = users;
    }

    public DepartureStatsDto Stats { get; private set; } = new(0, 0, 0, 0);
    public IReadOnlyList<(Guid Id, string Label)> Templates { get; private set; } = [];
    public IReadOnlyList<(Guid Id, string Name)> Users { get; private set; } = [];
    public IReadOnlyList<string> TourTypes { get; private set; } = [];

    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập mã chuyến")] public string Code { get; set; } = "";
        [Required(ErrorMessage = "Bắt buộc nhập tên chuyến")] public string Title { get; set; } = "";
        public Guid? TemplateId { get; set; }
        public DateTimeOffset? DepartureDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public int TotalSlots { get; set; }
    }

    public static string StatusLabel(bool isClosed) => isClosed ? "Đã đóng" : "Đang mở";
    public static string StatusColor(bool isClosed) => isClosed ? "danger" : "success";

    public async Task OnGetAsync()
    {
        Stats = await _svc.GetStatsAsync();
        TourTypes = (await _svc.GetFilterOptionsAsync()).TourTypes;
        Templates = (await _templates.ListAsync(1, 1000)).Items
            .Select(t => (t.Id, $"{t.Code} — {t.Title}")).ToList();
        Users = (await _users.ListAsync()).Select(u => (u.Id, u.FullName)).ToList();
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

        // Tên NV điều hành: chỉ tra cho user xuất hiện trong TRANG hiện tại.
        var userIds = result.Items.Where(d => d.AssignedToUserId is not null).Select(d => d.AssignedToUserId!.Value).ToHashSet();
        var names = userIds.Count == 0
            ? []
            : (await _users.ListAsync()).Where(u => userIds.Contains(u.Id)).ToDictionary(u => u.Id, u => u.FullName);

        var stats = await _svc.GetStatsAsync();
        var data = result.Items.Select(d => new
        {
            id = d.Id,
            code = d.Code,
            title = d.Title,
            tourType = d.TourType ?? "—",
            departureDateText = d.DepartureDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "—",
            endDateText = d.EndDate?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            totalSlots = d.TotalSlots,
            assigneeName = d.AssignedToUserId is Guid g && names.TryGetValue(g, out var n) ? n : "—",
            isClosed = d.IsClosed,
            statusLabel = StatusLabel(d.IsClosed),
            statusColor = StatusColor(d.IsClosed),
        }).ToList();

        return DtJson(dt.Draw, stats.Total, result.Total, data);
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        try
        {
            await _svc.CreateAsync(new CreateDepartureDto(
                Input.TemplateId, Input.Code, Input.Title,
                TkDate.Day(Input.DepartureDate), TkDate.Day(Input.EndDate), Input.TotalSlots));
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }

        return new JsonResult(Result.Success("Đã tạo chuyến đi."));
    }

    /// <summary>Đóng chuyến (khoá đặt chỗ) — trả Result để bảng server-side reload tại chỗ.</summary>
    public async Task<IActionResult> OnPostCloseAsync(Guid id)
    {
        try
        {
            await _svc.CloseAsync(id);
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }

        return new JsonResult(Result.Success("Đã đóng chuyến đi."));
    }
}
