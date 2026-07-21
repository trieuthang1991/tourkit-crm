using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Operations;
using TourKit.Application.Providers;
using TourKit.Application.Providers.Dtos;
using TourKit.Shared.Enums;

namespace TourKit.Api.Pages.GuideAssignments;

// Lịch điều HDV: DataTables SERVER-SIDE + GIỮ ĐỦ thông tin bản cũ
// (web/src/features/guides/GuideAssignmentsPage.tsx): 4 KPI, thanh lọc (HDV · chuyến · khoảng ngày đi),
// tab trạng thái, cột ghép Chuyến / HDV / Thời gian, cột Thu-chi (modal đối soát) + Bàn giao (modal biên bản),
// offcanvas CRUD đầy đủ (giữ nguyên Create/Update DTO + chống trùng lịch ở service).
[Authorize(Policy = "guide.view")]
public class IndexModel : TkListPageModel
{
    private readonly IGuideAssignmentService _svc;
    private readonly IDepartureService _departures;
    private readonly IProviderService _providers;
    private readonly IGuideTransactionService _tx;

    public IndexModel(
        IGuideAssignmentService svc,
        IDepartureService departures,
        IProviderService providers,
        IGuideTransactionService tx)
    {
        _svc = svc;
        _departures = departures;
        _providers = providers;
        _tx = tx;
    }

    public GuideAssignmentStatsDto Stats { get; private set; } = new(0, 0, 0, 0);
    public IReadOnlyList<(Guid Id, string Label)> Departures { get; private set; } = [];
    public IReadOnlyList<(Guid Id, string Name)> Guides { get; private set; } = [];

    public bool CanManage => User.HasClaim("perm", "guide.manage");

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc chọn chuyến")] public Guid? TourDepartureId { get; set; }
        [Required(ErrorMessage = "Bắt buộc chọn HDV")] public Guid? ProviderId { get; set; }
        public DateTimeOffset? TimeGo { get; set; }
        public DateTimeOffset? TimeCome { get; set; }
        public DateTimeOffset? TimeReturn { get; set; }
        public string? Note { get; set; }
        public int Status { get; set; } = 1;
    }

    // Trạng thái bám legacy State: 1 = đã điều (Created) · 2 = đang thực hiện (Active) · 4 = đã huỷ.
    public static string StatusLabel(int s) => s switch
    {
        1 => "Đã điều",
        2 => "Đang thực hiện",
        4 => "Đã huỷ",
        _ => "—",
    };

    public static string StatusColor(int s) => s switch
    {
        2 => "success",
        4 => "secondary",
        _ => "info",
    };

    public async Task OnGetAsync()
    {
        Stats = await _svc.GetStatsAsync();
        Departures = (await _departures.ListAsync(1, 1000)).Items
            .Select(d => (d.Id, $"{d.Code} — {d.Title}")).ToList();
        Guides = (await _providers.ListAsync(1, 1000, new ProviderListFilter(Type: (int)ProviderType.Guide))).Items
            .Select(p => (p.Id, p.Name)).ToList();
    }

    /// <summary>Dựng bộ lọc từ query — đúng các tiêu chí GuideAssignmentListFilter hỗ trợ.</summary>
    private GuideAssignmentListFilter BuildFilter()
    {
        var q = Request.Query;
        int? I(string k) => int.TryParse(q[k], out var n) ? n : null;
        Guid? G(string k) => Guid.TryParse(q[k], out var g) ? g : null;
        DateTimeOffset? D(string k) => DateTimeOffset.TryParse(q[k], CultureInfo.InvariantCulture, out var d) ? d.ToUniversalTime() : null;

        return new GuideAssignmentListFilter(
            ProviderId: G("providerId"),
            DepartureId: G("departureId"),
            Status: I("status"),
            DateFrom: D("dateFrom"),
            DateTo: D("dateTo"));
    }

    /// <summary>Nguồn DataTables server-side: chỉ trả đúng 1 trang.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var result = await _svc.ListAsync(dt.Page, dt.Size, BuildFilter());
        var stats = await _svc.GetStatsAsync();

        var data = result.Items.Select(x => new
        {
            id = x.Id,
            tourDepartureId = x.TourDepartureId,
            providerId = x.ProviderId,
            providerName = string.IsNullOrWhiteSpace(x.ProviderName) ? "—" : x.ProviderName,
            departureCode = x.DepartureCode ?? "—",
            departureTitle = string.IsNullOrWhiteSpace(x.DepartureTitle) ? "—" : x.DepartureTitle,
            // yyyy-MM-dd để prefill flatpickr trong offcanvas; *Text để hiển thị.
            timeGo = x.TimeGo?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            timeCome = x.TimeCome?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            timeReturn = x.TimeReturn?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            timeGoText = x.TimeGo?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "—",
            timeComeText = x.TimeCome?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            timeReturnText = x.TimeReturn?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            note = x.Note,
            status = x.Status,
            statusLabel = StatusLabel(x.Status),
            statusColor = StatusColor(x.Status),
            handoverContent = x.HandoverContent,
            handedOverAtText = x.HandedOverAt?.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
        }).ToList();

        // Kèm khối stats ngoài contract DataTables → KPI/tab tự làm tươi qua sự kiện xhr.dt (không thêm roundtrip).
        return new JsonResult(new
        {
            draw = dt.Draw,
            recordsTotal = stats.Total,
            recordsFiltered = result.Total,
            data,
            stats = new { total = stats.Total, created = stats.Created, active = stats.Active, guideCount = stats.GuideCount },
        });
    }

    /// <summary>Đối soát thu-chi của MỘT phân công (mở theo dòng — không nạp toàn bộ).</summary>
    public async Task<IActionResult> OnGetSettlementAsync(Guid id)
    {
        var s = await _tx.GetByAssignmentAsync(id);
        return new JsonResult(new
        {
            totalRevenue = s.TotalRevenue,
            totalExpense = s.TotalExpense,
            net = s.Net,
            items = s.Items.Select(i => new
            {
                id = i.Id,
                type = i.Type,
                typeLabel = i.Type == (int)GuideTransactionType.Expense ? "Chi" : "Thu",
                amount = i.Amount,
                description = i.Description,
                occurredAtText = i.OccurredAt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            }).ToList(),
        });
    }

    public async Task<IActionResult> OnPostAddTransactionAsync(Guid id, int type, decimal amount, string? description)
    {
        if (!CanManage)
        {
            return new JsonResult(Result.Error("Bạn không có quyền ghi thu-chi HDV."));
        }

        if (amount <= 0 || string.IsNullOrWhiteSpace(description))
        {
            return new JsonResult(Result.Error("Nhập số tiền > 0 và diễn giải."));
        }

        try
        {
            await _tx.CreateAsync(id, new CreateGuideTransactionDto(type, amount, description.Trim(), null));
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }

        return new JsonResult(Result.Success("Đã ghi thu-chi."));
    }

    public async Task<IActionResult> OnPostDeleteTransactionAsync(Guid id, Guid transactionId)
    {
        if (!CanManage)
        {
            return new JsonResult(Result.Error("Bạn không có quyền xoá thu-chi HDV."));
        }

        try
        {
            await _tx.DeleteAsync(id, transactionId);
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }

        return new JsonResult(Result.Success("Đã xoá dòng thu-chi."));
    }

    /// <summary>Biên bản bàn giao sau tour (legacy HandoverNote).</summary>
    public async Task<IActionResult> OnPostHandoverAsync(Guid id, string? content)
    {
        if (!CanManage)
        {
            return new JsonResult(Result.Error("Bạn không có quyền nộp bàn giao."));
        }

        try
        {
            await _svc.HandoverAsync(id, new HandoverDto(content ?? ""));
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }

        return new JsonResult(Result.Success("Đã nộp biên bản bàn giao."));
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!CanManage)
        {
            return new JsonResult(Result.Error("Bạn không có quyền sửa phân công HDV."));
        }

        if (!ModelState.IsValid || Input.ProviderId is not Guid providerId)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        try
        {
            if (Id is Guid g && g != Guid.Empty)
            {
                await _svc.UpdateAsync(g, new UpdateGuideAssignmentDto(
                    providerId, TkDate.Day(Input.TimeGo), TkDate.Day(Input.TimeCome),
                    TkDate.Day(Input.TimeReturn), Input.Note, Input.Status));
            }
            else
            {
                if (Input.TourDepartureId is not Guid departureId)
                {
                    return new JsonResult(Result.Error("Bắt buộc chọn chuyến."));
                }

                await _svc.CreateAsync(new CreateGuideAssignmentDto(
                    departureId, providerId, TkDate.Day(Input.TimeGo), TkDate.Day(Input.TimeCome),
                    TkDate.Day(Input.TimeReturn), Input.Note, Input.Status));
            }
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }

        return new JsonResult(Result.Success("Đã lưu phân công HDV."));
    }

    /// <summary>Xoá — trả Result để bảng server-side reload tại chỗ (không postback).</summary>
    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        if (!CanManage)
        {
            return new JsonResult(Result.Error("Bạn không có quyền xoá phân công HDV."));
        }

        try
        {
            await _svc.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }

        return new JsonResult(Result.Success("Đã xoá phân công HDV."));
    }
}
