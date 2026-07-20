using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;

namespace TourKit.Api.Pages.VehiclePending;

// Lịch xe chờ duyệt — DataTables SERVER-SIDE (không get-all).
// Bản cũ (web/src/features/vehicleAssignments/VehicleSchedulePage.tsx) là GANTT: nạp 500 bản ghi rồi gom
// theo xe ở client → vi phạm "không get-all". Ở đây thay bằng BẢNG server-side + lọc khoảng ngày đón,
// giữ đủ thông tin của thanh Gantt (xe = dòng, chuyến + tài xế = nhãn thanh, màu = trạng thái).
// Nghiệp vụ phân xe KHÔNG có luồng duyệt riêng: Status bám legacy State (1 = Created "chờ đưa vào vận hành",
// 2 = Active, 4 = Delete) → "chờ duyệt" = Status 1. Kích hoạt/sửa/xoá làm ở màn Lịch điều xe.
[Authorize(Policy = "vehicle.view")]
public class IndexModel : TkListPageModel
{
    private const int PendingStatus = 1; // Created = chờ đưa vào vận hành (chưa Active)

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

    public static string StatusLabel(int s) => s switch
    {
        1 => "Chờ duyệt",
        2 => "Đã điều",
        4 => "Đã huỷ",
        _ => "—",
    };

    public static string StatusColor(int s) => s switch
    {
        2 => "success",
        4 => "secondary",
        _ => "warning",
    };

    public async Task OnGetAsync()
    {
        Stats = await _svc.GetStatsAsync();
        Departures = (await _departures.ListAsync(1, 1000)).Items
            .Select(d => (d.Id, $"{d.Code} — {d.Title}")).ToList();
        Vehicles = (await _vehicles.ListAsync(1, 1000)).Items
            .Select(v => (v.Id, $"{v.Name} ({v.SeatType} chỗ)")).ToList();
    }

    /// <summary>Dựng bộ lọc từ query — đúng các tiêu chí VehicleAssignmentListFilter hỗ trợ.
    /// Mặc định Status = chờ duyệt; người dùng có thể đổi qua ô trạng thái (bám Select hệ cũ).</summary>
    private VehicleAssignmentListFilter BuildFilter()
    {
        var q = Request.Query;
        Guid? G(string k) => Guid.TryParse(q[k], out var g) ? g : null;
        DateTimeOffset? D(string k) => DateTimeOffset.TryParse(q[k], CultureInfo.InvariantCulture, out var d) ? d.ToUniversalTime() : null;

        return new VehicleAssignmentListFilter(
            VehicleId: G("vehicleId"),
            DepartureId: G("departureId"),
            Status: int.TryParse(q["status"], out var st) ? st : PendingStatus,
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
            vehicleName = string.IsNullOrWhiteSpace(x.VehicleName) ? "—" : x.VehicleName,
            departureCode = x.DepartureCode ?? "—",
            departureTitle = string.IsNullOrWhiteSpace(x.DepartureTitle) ? "—" : x.DepartureTitle,
            driverName = x.DriverName,
            driverPhone = x.DriverPhone,
            timeGoText = x.TimeGo?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "—",
            timeComeText = x.TimeCome?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
            note = x.Note,
            status = x.Status,
            statusLabel = StatusLabel(x.Status),
            statusColor = StatusColor(x.Status),
        }).ToList();

        // Kèm khối stats ngoài contract DataTables → KPI tự làm tươi qua sự kiện xhr.dt (không thêm roundtrip).
        return new JsonResult(new
        {
            draw = dt.Draw,
            recordsTotal = stats.Total,
            recordsFiltered = result.Total,
            data,
            stats = new { total = stats.Total, created = stats.Created, active = stats.Active, vehicleCount = stats.VehicleCount },
        });
    }
}
