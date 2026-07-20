using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;

namespace TourKit.Api.Pages.ServiceOperations;

// READ-ONLY + hành động: IServiceOperationService là lớp ĐỌC trên ServiceBooking, chỉ có mutation duy nhất là
// PayAsync (ghi nhận số đã chi NCC) — không Create/Update. Danh sách + StatCards + offcanvas "Ghi nhận thanh toán".
[Authorize(Policy = "servicebooking.view")]
public class IndexModel : PageModel
{
    private readonly IServiceOperationService _svc;
    public IndexModel(IServiceOperationService svc) => _svc = svc;

    public IReadOnlyList<ServiceOperationDto> Items { get; private set; } = [];
    public ServiceOperationStatsDto Stats { get; private set; } = new(0, 0, 0, 0, 0, 0, 0);

    [BindProperty] public Guid Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Range(0, double.MaxValue, ErrorMessage = "Số tiền phải >= 0")] public decimal PaidAmount { get; set; }
    }

    public static string PaymentLabel(int s) => s switch
    {
        0 => "Chờ chi",
        1 => "Chưa chi hết",
        2 => "Hoàn thành",
        _ => "—",
    };

    public static string PaymentColor(int s) => s switch
    {
        2 => "success",
        1 => "warning",
        _ => "secondary",
    };

    public async Task OnGetAsync()
    {
        Stats = await _svc.GetStatsAsync();
        Items = (await _svc.ListAsync(1, 1000)).Items;
    }

    public async Task<IActionResult> OnPostPayAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        try
        {
            await _svc.PayAsync(Id, new PayServiceOperationDto(Input.PaidAmount));
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }

        return new JsonResult(Result.Success("Đã ghi nhận thanh toán."));
    }
}
