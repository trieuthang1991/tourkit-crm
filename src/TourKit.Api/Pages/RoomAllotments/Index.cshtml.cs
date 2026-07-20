using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.Rooms;
using TourKit.Application.Rooms.Dtos;

namespace TourKit.Api.Pages.RoomAllotments;

// CRUD offcanvas: Create/Update DTO toàn scalar (ProviderRef/ServiceName/Province/Market là chuỗi, không FK Guid;
// DayType/Quota/Booked/Price/Rating là số) → dựng form trực tiếp, không cần lookup Guid.
[Authorize(Policy = "roomfund.view")]
public class IndexModel : PageModel
{
    private readonly IRoomAllotmentService _svc;
    public IndexModel(IRoomAllotmentService svc) => _svc = svc;

    public IReadOnlyList<RoomAllotmentDto> Items { get; private set; } = [];
    public RoomAllotmentStatsDto Stats { get; private set; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0);

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
        Items = (await _svc.ListAsync(1, 1000)).Items;
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

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _svc.DeleteAsync(id);
            TempData["ok"] = "Đã xoá ô quỹ phòng.";
        }
        catch (Exception ex)
        {
            TempData["err"] = ex.Message;
        }
        return RedirectToPage();
    }
}
