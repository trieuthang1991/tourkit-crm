using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;

namespace TourKit.Api.Pages.Vehicles;

// Kho xe: DataTables SERVER-SIDE + GIỮ ĐỦ thông tin bản cũ
// (web/src/features/vehicles/VehiclesPage.tsx): cột Tên xe, Hãng, Số chỗ, Trạng thái.
// IVehicleService.ListAsync CHỈ nhận (page, size) → không có tiêu chí lọc nào để đẩy xuống SQL.
[Authorize(Policy = "vehicle.view")]
public class IndexModel : TkListPageModel
{
    private readonly IVehicleService _svc;
    public IndexModel(IVehicleService svc) => _svc = svc;

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập tên xe")] public string Name { get; set; } = "";
        public string? FirmName { get; set; }
        public int SeatType { get; set; }
        public int Status { get; set; } = 1;
    }

    public static string StatusLabel(int s) => s == 1 ? "Hoạt động" : "Ngừng";

    /// <summary>Nguồn DataTables server-side: chỉ trả đúng 1 trang + đủ field cho offcanvas sửa.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var result = await _svc.ListAsync(dt.Page, dt.Size, dt.Keyword);

        var data = result.Items.Select(v => new
        {
            id = v.Id,
            name = v.Name,
            firmName = v.FirmName,
            seatType = v.SeatType,
            status = v.Status,
            statusLabel = StatusLabel(v.Status),
        });

        return DtJson(dt.Draw, result.Total, result.Total, data);
    }

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
