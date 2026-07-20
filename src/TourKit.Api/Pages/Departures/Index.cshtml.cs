using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.Booking;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Catalog;

namespace TourKit.Api.Pages.Departures;

[Authorize(Policy = "departure.view")]
public class IndexModel : PageModel
{
    private readonly IDepartureService _svc;
    private readonly ITourTemplateService _templates;
    public IndexModel(IDepartureService svc, ITourTemplateService templates)
    {
        _svc = svc;
        _templates = templates;
    }

    public IReadOnlyList<DepartureDto> Items { get; private set; } = [];
    public DepartureStatsDto Stats { get; private set; } = new(0, 0, 0, 0);
    public IReadOnlyList<(Guid Id, string Label)> Templates { get; private set; } = [];

    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập mã chuyến")] public string Code { get; set; } = "";
        [Required(ErrorMessage = "Bắt buộc nhập tên chuyến")] public string Title { get; set; } = "";
        public Guid? TemplateId { get; set; }
        public DateTimeOffset? DepartureDate { get; set; }
        public DateTimeOffset? EndDate { get; set; }
        public int TotalSlots { get; set; }
    }

    public static string StatusLabel(DepartureDto d) => d.IsClosed ? "Đã đóng" : "Đang mở";
    public static string StatusColor(DepartureDto d) => d.IsClosed ? "secondary" : "success";

    public async Task OnGetAsync()
    {
        Stats = await _svc.GetStatsAsync();
        Items = (await _svc.ListAsync(1, 1000)).Items;
        Templates = (await _templates.ListAsync(1, 1000)).Items
            .Select(t => (t.Id, $"{t.Code} — {t.Title}")).ToList();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        try
        {
            await _svc.CreateAsync(new CreateDepartureDto(
                Input.TemplateId, Input.Code, Input.Title,
                Input.DepartureDate?.ToUniversalTime(), Input.EndDate?.ToUniversalTime(), Input.TotalSlots));
        }
        catch (Exception ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }

        return new JsonResult(Result.Success("Đã tạo chuyến đi."));
    }

    public async Task<IActionResult> OnPostCloseAsync(Guid id)
    {
        try
        {
            await _svc.CloseAsync(id);
            TempData["ok"] = "Đã đóng chuyến đi.";
        }
        catch (Exception ex)
        {
            TempData["err"] = ex.Message;
        }
        return RedirectToPage();
    }
}
