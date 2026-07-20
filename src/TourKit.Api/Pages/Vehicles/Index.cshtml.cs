using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;

namespace TourKit.Api.Pages.Vehicles;

[Authorize(Policy = "vehicle.view")]
public class IndexModel : PageModel
{
    private readonly IVehicleService _svc;
    public IndexModel(IVehicleService svc) => _svc = svc;

    public IReadOnlyList<VehicleDto> Items { get; private set; } = [];
    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập tên xe")] public string Name { get; set; } = "";
        public string? FirmName { get; set; }
        public int SeatType { get; set; }
        public int Status { get; set; } = 1;
    }

    public async Task OnGetAsync() => Items = (await _svc.ListAsync(1, 1000)).Items;

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        if (Id is Guid g && g != Guid.Empty)
        {
            await _svc.UpdateAsync(g, new UpdateVehicleDto(Input.Name, Input.FirmName, Input.SeatType, Input.Status));
        }
        else
        {
            await _svc.CreateAsync(new CreateVehicleDto(Input.Name, Input.FirmName, Input.SeatType, Input.Status));
        }

        return new JsonResult(Result.Success("Đã lưu xe."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá xe.";
        return RedirectToPage();
    }
}
