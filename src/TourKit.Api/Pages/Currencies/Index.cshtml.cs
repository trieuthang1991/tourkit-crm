using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Api.Pages.Currencies;

[Authorize(Policy = "service.view")]
public class IndexModel : PageModel
{
    private readonly ICurrencyService _svc;
    public IndexModel(ICurrencyService svc) => _svc = svc;

    public IReadOnlyList<CurrencyDto> Items { get; private set; } = [];
    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập mã")] public string Code { get; set; } = "";
        [Required(ErrorMessage = "Bắt buộc nhập tên")] public string Name { get; set; } = "";
        public decimal RateToVnd { get; set; }
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
            await _svc.UpdateAsync(g, new UpdateCurrencyDto(Input.Code, Input.Name, Input.RateToVnd, Input.SortOrder));
        }
        else
        {
            await _svc.CreateAsync(new CreateCurrencyDto(Input.Code, Input.Name, Input.RateToVnd, Input.SortOrder));
        }

        return new JsonResult(Result.Success("Đã lưu tiền tệ."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá tiền tệ.";
        return RedirectToPage();
    }
}
