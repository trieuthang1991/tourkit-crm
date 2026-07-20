using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;

namespace TourKit.Api.Pages.VehicleAssignments;

// CRUD offcanvas: Create/Update DTO gồm FK scalar (TourDepartureId, VehicleId) đều có lookup enrich tên
// (chuyến qua IDepartureService, xe qua IVehicleService) + tài xế/giờ/ghi chú/trạng thái scalar.
[Authorize(Policy = "vehicle.view")]
public class IndexModel : PageModel
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

    public IReadOnlyList<VehicleAssignmentDto> Items { get; private set; } = [];
    public VehicleAssignmentStatsDto Stats { get; private set; } = new(0, 0, 0, 0);
    public IReadOnlyList<(Guid Id, string Label)> Departures { get; private set; } = [];
    public IReadOnlyList<(Guid Id, string Label)> Vehicles { get; private set; } = [];

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
        Items = (await _svc.ListAsync(1, 1000)).Items;
        Departures = (await _departures.ListAsync(1, 1000)).Items
            .Select(d => (d.Id, $"{d.Code} — {d.Title}")).ToList();
        Vehicles = (await _vehicles.ListAsync(1, 1000)).Items
            .Select(v => (v.Id, $"{v.Name} ({v.SeatType} chỗ)")).ToList();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
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
                    Input.TimeGo?.ToUniversalTime(), Input.TimeCome?.ToUniversalTime(), Input.Note, Input.Status));
            }
            else
            {
                if (Input.TourDepartureId is not Guid departureId)
                {
                    return new JsonResult(Result.Error("Bắt buộc chọn chuyến."));
                }

                await _svc.CreateAsync(new CreateVehicleAssignmentDto(
                    departureId, vehicleId, Input.DriverName, Input.DriverPhone,
                    Input.TimeGo?.ToUniversalTime(), Input.TimeCome?.ToUniversalTime(), Input.Note, Input.Status));
            }
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }

        return new JsonResult(Result.Success("Đã lưu phân xe."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _svc.DeleteAsync(id);
            TempData["ok"] = "Đã xoá phân xe.";
        }
        catch (Exception ex)
        {
            TempData["err"] = ex.Message;
        }
        return RedirectToPage();
    }
}
