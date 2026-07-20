using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Providers;
using TourKit.Application.Providers.Dtos;
using TourKit.Shared.Enums;

namespace TourKit.Api.Pages.GuideAssignments;

// CRUD offcanvas: Create/Update DTO gồm FK scalar (TourDepartureId, ProviderId) đều có lookup enrich tên
// (chuyến qua IDepartureService, HDV qua IProviderService lọc ProviderType.Guide) + giờ/ghi chú/trạng thái scalar.
[Authorize(Policy = "guide.view")]
public class IndexModel : PageModel
{
    private readonly IGuideAssignmentService _svc;
    private readonly IDepartureService _departures;
    private readonly IProviderService _providers;
    public IndexModel(IGuideAssignmentService svc, IDepartureService departures, IProviderService providers)
    {
        _svc = svc;
        _departures = departures;
        _providers = providers;
    }

    public IReadOnlyList<GuideAssignmentDto> Items { get; private set; } = [];
    public GuideAssignmentStatsDto Stats { get; private set; } = new(0, 0, 0, 0);
    public IReadOnlyList<(Guid Id, string Label)> Departures { get; private set; } = [];
    public IReadOnlyList<(Guid Id, string Name)> Guides { get; private set; } = [];

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
        Guides = (await _providers.ListAsync(1, 1000, new ProviderListFilter(Type: (int)ProviderType.Guide))).Items
            .Select(p => (p.Id, p.Name)).ToList();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid || Input.ProviderId is not Guid providerId)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        try
        {
            if (Id is Guid g && g != Guid.Empty)
            {
                await _svc.UpdateAsync(g, new UpdateGuideAssignmentDto(
                    providerId, Input.TimeGo?.ToUniversalTime(), Input.TimeCome?.ToUniversalTime(),
                    Input.TimeReturn?.ToUniversalTime(), Input.Note, Input.Status));
            }
            else
            {
                if (Input.TourDepartureId is not Guid departureId)
                {
                    return new JsonResult(Result.Error("Bắt buộc chọn chuyến."));
                }

                await _svc.CreateAsync(new CreateGuideAssignmentDto(
                    departureId, providerId, Input.TimeGo?.ToUniversalTime(), Input.TimeCome?.ToUniversalTime(),
                    Input.TimeReturn?.ToUniversalTime(), Input.Note, Input.Status));
            }
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }

        return new JsonResult(Result.Success("Đã lưu phân công HDV."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await _svc.DeleteAsync(id);
            TempData["ok"] = "Đã xoá phân công HDV.";
        }
        catch (Exception ex)
        {
            TempData["err"] = ex.Message;
        }
        return RedirectToPage();
    }
}
