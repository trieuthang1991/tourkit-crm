using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.Crm;
using TourKit.Application.Crm.Dtos;

namespace TourKit.Api.Pages.TourRatings;

[Authorize(Policy = "rating.view")]
public class IndexModel : PageModel
{
    private readonly ITourRatingService _svc;
    public IndexModel(ITourRatingService svc) => _svc = svc;

    public IReadOnlyList<TourRatingDto> Items { get; private set; } = [];
    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public int Stars { get; set; } = 5;
        public string? Comment { get; set; }
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
            await _svc.UpdateAsync(g, new UpdateTourRatingDto(Input.CustomerName, Input.CustomerPhone, Input.Stars, Input.Comment, Input.Status));
        }
        else
        {
            await _svc.CreateAsync(new CreateTourRatingDto(null, null, Input.CustomerName, Input.CustomerPhone, Input.Stars, Input.Comment, Input.Status));
        }

        return new JsonResult(Result.Success("Đã lưu đánh giá tour."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá đánh giá tour.";
        return RedirectToPage();
    }
}
