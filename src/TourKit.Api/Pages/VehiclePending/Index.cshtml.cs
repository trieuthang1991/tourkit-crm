using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;

using TourKit.Api.Web;
namespace TourKit.Api.Pages.VehiclePending;

// Lịch xe chờ duyệt — 2 khung: LỊCH (FullCalendar của template) và BẢNG (DataTables server-side).
// Bản cũ (web/src/features/vehicleAssignments/VehicleSchedulePage.tsx) là GANTT: nạp 500 bản ghi rồi gom
// theo xe ở client → vi phạm "không get-all". Ở đây giữ ĐÚNG dạng lịch nhưng handler Events chỉ nạp phân xe
// TRONG KHOẢNG ĐANG XEM (FullCalendar gửi start/end mỗi lần đổi tháng), trần 500 sự kiện/khung;
// khung bảng giữ phân trang server + lọc. Thông tin thanh Gantt giữ nguyên (xe = tiêu đề, chuyến + tài xế,
// màu = trạng thái).
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
        Departures = (await _departures.ListAsync(1, TranDanhMuc.Chuyen)).Items
            .Select(d => (d.Id, $"{d.Code} — {d.Title}")).ToList();
        Vehicles = (await _vehicles.ListAsync(1, TranDanhMuc.Xe)).Items
            .Select(v => (v.Id, $"{v.Name} ({v.SeatType} chỗ)")).ToList();
    }

    /// <summary>Dựng bộ lọc từ query — đúng các tiêu chí VehicleAssignmentListFilter hỗ trợ.
    /// Mặc định Status = chờ duyệt; người dùng có thể đổi qua ô trạng thái (bám Select hệ cũ).
    /// windowFrom/windowTo (khung lịch đang xem) nếu có sẽ ĐÈ khoảng ngày của thanh lọc.</summary>
    private VehicleAssignmentListFilter BuildFilter(DateTimeOffset? windowFrom = null, DateTimeOffset? windowTo = null, string? keyword = null)
    {
        var q = Request.Query;
        Guid? G(string k) => Guid.TryParse(q[k], out var g) ? g : null;
        DateTimeOffset? D(string k) => DateTimeOffset.TryParse(q[k], CultureInfo.InvariantCulture, out var d) ? d.ToUniversalTime() : null;

        return new VehicleAssignmentListFilter(
            VehicleId: G("vehicleId"),
            DepartureId: G("departureId"),
            Status: int.TryParse(q["status"], out var st) ? st : PendingStatus,
            DateFrom: windowFrom?.ToUniversalTime() ?? D("dateFrom"),
            DateTo: windowTo?.ToUniversalTime() ?? D("dateTo"),
            Q: keyword);
    }

    /// <summary>Nguồn DataTables server-side: chỉ trả đúng 1 trang.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var result = await _svc.ListAsync(dt.Page, dt.Size, BuildFilter(keyword: dt.Keyword));
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

    /// <summary>
    /// Nguồn sự kiện cho FullCalendar: CHỈ nạp phân xe có ngày đón nằm trong khoảng lịch đang xem
    /// (FullCalendar gửi start/end của khung hiện tại) — không get-all. Trần 500 sự kiện/khung;
    /// vượt trần thì trả cờ truncated để UI nhắc thu hẹp bộ lọc.
    /// </summary>
    public async Task<IActionResult> OnGetEventsAsync(DateTimeOffset? start, DateTimeOffset? end)
    {
        const int max = 500;
        var result = await _svc.ListAsync(1, max, BuildFilter(start, end));

        var events = result.Items
            .Where(x => x.TimeGo is not null)
            .Select(x => new
            {
                id = x.Id,
                // Tiêu đề = biển số/tên xe + mã chuyến (đúng nhãn thanh Gantt hệ cũ).
                title = $"{(string.IsNullOrWhiteSpace(x.VehicleName) ? "—" : x.VehicleName)} · {x.DepartureCode ?? "—"}",
                start = x.TimeGo!.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                // FullCalendar coi 'end' là mốc loại trừ → +1 ngày để thanh phủ hết ngày trả xe.
                end = x.TimeCome?.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                className = "bg-label-" + StatusColor(x.Status),
                extendedProps = new
                {
                    vehicleName = string.IsNullOrWhiteSpace(x.VehicleName) ? "—" : x.VehicleName,
                    departureCode = x.DepartureCode ?? "—",
                    departureTitle = string.IsNullOrWhiteSpace(x.DepartureTitle) ? "—" : x.DepartureTitle,
                    driverName = string.IsNullOrWhiteSpace(x.DriverName) ? "—" : x.DriverName,
                    driverPhone = x.DriverPhone,
                    statusLabel = StatusLabel(x.Status),
                    timeComeText = x.TimeCome?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture),
                },
            })
            .ToList();

        return new JsonResult(new { events, truncated = result.Total > max, total = result.Total });
    }
}
