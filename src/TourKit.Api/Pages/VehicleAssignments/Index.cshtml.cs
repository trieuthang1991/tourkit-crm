using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;

namespace TourKit.Api.Pages.VehicleAssignments;

// Lịch điều xe: DataTables SERVER-SIDE + GIỮ ĐỦ thông tin bản cũ
// (web/src/features/vehicleAssignments/VehicleAssignmentsPage.tsx): 4 KPI, thanh lọc (xe · chuyến ·
// khoảng ngày đón), tab trạng thái, cột ghép Chuyến / Xe / Tài xế / Thời gian,
// offcanvas CRUD đầy đủ (giữ nguyên Create/Update DTO + chống trùng lịch ở service).
[Authorize(Policy = "vehicle.view")]
public class IndexModel : TkListPageModel
{
    private readonly IVehicleAssignmentService _svc;
    private readonly IDepartureService _departures;
    private readonly IVehicleService _vehicles;

    public IndexModel(IVehicleAssignmentService svc, IDepartureService departures, IVehicleService vehicles)
    {
        _svc = svc;
        _departures = departures;
        _vehicles = vehicles;
    }

    public VehicleAssignmentStatsDto Stats { get; private set; } = new(0, 0, 0, 0);
    public IReadOnlyList<(Guid Id, string Label)> Departures { get; private set; } = [];
    public IReadOnlyList<(Guid Id, string Label)> Vehicles { get; private set; } = [];

    public bool CanManage => User.HasClaim("perm", "vehicle.manage");

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc chọn chuyến")] public Guid? TourDepartureId { get; set; }
        [Required(ErrorMessage = "Bắt buộc chọn xe")] public Guid? VehicleId { get; set; }
        public string? DriverName { get; set; }
        public string? DriverPhone { get; set; }
        public DateTimeOffset? TimeGo { get; set; }
        public DateTimeOffset? TimeCome { get; set; }
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
        Departures = (await _departures.ListAsync(1, TranDanhMuc.Chuyen)).Items
            .Select(d => (d.Id, $"{d.Code} — {d.Title}")).ToList();
        Vehicles = (await _vehicles.ListAsync(1, TranDanhMuc.Xe)).Items
            .Select(v => (v.Id, $"{v.Name} ({v.SeatType} chỗ)")).ToList();
    }

    /// <summary>Dựng bộ lọc từ query — đúng các tiêu chí VehicleAssignmentListFilter hỗ trợ.</summary>
    private VehicleAssignmentListFilter BuildFilter(string? keyword = null)
    {
        var q = Request.Query;
        int? I(string k) => int.TryParse(q[k], out var n) ? n : null;
        Guid? G(string k) => Guid.TryParse(q[k], out var g) ? g : null;
        DateTimeOffset? D(string k) => DateTimeOffset.TryParse(q[k], CultureInfo.InvariantCulture, out var d) ? d.ToUniversalTime() : null;

        return new VehicleAssignmentListFilter(
            VehicleId: G("vehicleId"),
            DepartureId: G("departureId"),
            Status: I("status"),
            DateFrom: D("dateFrom"),
            DateTo: D("dateTo"),
            Q: keyword);
    }

    /// <summary>Nguồn DataTables server-side: chỉ trả đúng 1 trang.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var result = await _svc.ListAsync(dt.Page, dt.Size, BuildFilter(dt.Keyword));
        var stats = await _svc.GetStatsAsync();

        var data = result.Items.Select(x => new
        {
            id = x.Id,
            tourDepartureId = x.TourDepartureId,
            vehicleId = x.VehicleId,
            vehicleName = string.IsNullOrWhiteSpace(x.VehicleName) ? "—" : x.VehicleName,
            departureCode = x.DepartureCode ?? "—",
            departureTitle = string.IsNullOrWhiteSpace(x.DepartureTitle) ? "—" : x.DepartureTitle,
            driverName = x.DriverName,
            driverPhone = x.DriverPhone,
            // yyyy-MM-dd để prefill flatpickr trong offcanvas; *Text để hiển thị.
            timeGo = x.TimeGo?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            timeCome = x.TimeCome?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            timeGoText = x.TimeGo?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "—",
            timeComeText = x.TimeCome?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            note = x.Note,
            status = x.Status,
            statusLabel = StatusLabel(x.Status),
            statusColor = StatusColor(x.Status),
        }).ToList();

        // Kèm khối stats ngoài contract DataTables → KPI/tab tự làm tươi qua sự kiện xhr.dt (không thêm roundtrip).
        return new JsonResult(new
        {
            draw = dt.Draw,
            recordsTotal = stats.Total,
            recordsFiltered = result.Total,
            data,
            stats = new { total = stats.Total, created = stats.Created, active = stats.Active, vehicleCount = stats.VehicleCount },
        });
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!CanManage)
        {
            return new JsonResult(Result.Error("Bạn không có quyền sửa phân xe."));
        }

        if (!ModelState.IsValid || Input.VehicleId is not Guid vehicleId)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        try
        {
            if (Id is Guid g && g != Guid.Empty)
            {
                await _svc.UpdateAsync(g, new UpdateVehicleAssignmentDto(
                    vehicleId, Input.DriverName, Input.DriverPhone,
                    TkDate.Day(Input.TimeGo), TkDate.Day(Input.TimeCome), Input.Note, Input.Status));
            }
            else
            {
                if (Input.TourDepartureId is not Guid departureId)
                {
                    return new JsonResult(Result.Error("Bắt buộc chọn chuyến."));
                }

                await _svc.CreateAsync(new CreateVehicleAssignmentDto(
                    departureId, vehicleId, Input.DriverName, Input.DriverPhone,
                    TkDate.Day(Input.TimeGo), TkDate.Day(Input.TimeCome), Input.Note, Input.Status));
            }
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }

        return new JsonResult(Result.Success("Đã lưu phân xe."));
    }

    /// <summary>Xoá — trả Result để bảng server-side reload tại chỗ (không postback).</summary>
    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        if (!CanManage)
        {
            return new JsonResult(Result.Error("Bạn không có quyền xoá phân xe."));
        }

        try
        {
            await _svc.DeleteAsync(id);
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }

        return new JsonResult(Result.Success("Đã xoá phân xe."));
    }
}
