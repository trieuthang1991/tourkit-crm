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

// Quỹ phòng / allotment: DataTables SERVER-SIDE + GIỮ ĐỦ thông tin bản cũ
// (web/src/features/rooms/RoomFundPage.tsx): 5 KPI, chú giải loại ngày, 7 tiêu chí lọc
// (từ khoá · tỉnh thành · thị trường · NCC · hạng sao · khoảng ngày), cột ghép NCC/Dịch vụ,
// Tồn/Đặt/Còn + giá NET, dòng tổng cộng, offcanvas CRUD + xoá.
[Authorize(Policy = "roomfund.view")]
public class IndexModel : TkListPageModel
{
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

    public async Task OnGetAsync()
    {
        Stats = await _svc.GetStatsAsync();
        Providers = (await _providers.ListAsync(1, 1000)).Items.Select(p => (p.Id, p.Name)).ToList();
    }

    /// <summary>Dựng bộ lọc từ query — đúng các tiêu chí RoomAllotmentListFilter hỗ trợ.</summary>
    private RoomAllotmentListFilter BuildFilter(string? keyword)
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
            DateFrom: D("dateFrom"),
            DateTo: D("dateTo"));
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
