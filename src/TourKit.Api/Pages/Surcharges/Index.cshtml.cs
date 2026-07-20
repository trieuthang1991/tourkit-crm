using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Api.Pages.Surcharges;

[Authorize(Policy = "booking.view")]
public class IndexModel : PageModel
{
    private readonly ISurchargeService _svc;
    public IndexModel(ISurchargeService svc) => _svc = svc;

    public IReadOnlyList<SurchargeDto> Items { get; private set; } = [];
    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập tên")] public string Name { get; set; } = "";
        public int CalcType { get; set; }
        public decimal DefaultValue { get; set; }
        public int SortOrder { get; set; }
    }

    public async Task OnGetAsync() => Items = await _svc.ListAsync();

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        if (Id is Guid g && g != Guid.Empty)
        {
            await _svc.UpdateAsync(g, new UpdateSurchargeDto(Input.Name, Input.CalcType, Input.DefaultValue, Input.SortOrder));
        }
        else
        {
            await _svc.CreateAsync(new CreateSurchargeDto(Input.Name, Input.CalcType, Input.DefaultValue, Input.SortOrder));
        }

        return new JsonResult(Result.Success("Đã lưu phụ thu."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá phụ thu.";
        return RedirectToPage();
    }
}
