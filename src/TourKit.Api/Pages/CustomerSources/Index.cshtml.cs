using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Api.Pages.CustomerSources;

[Authorize(Policy = "customertype.view")]
public class IndexModel : PageModel
{
    private readonly ICustomerSourceService _svc;
    public IndexModel(ICustomerSourceService svc) => _svc = svc;

    public IReadOnlyList<CustomerSourceDto> Items { get; private set; } = [];

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập tên nguồn")]
        public string Name { get; set; } = "";
        public int SortOrder { get; set; }
    }

    public async Task OnGetAsync() => Items = await _svc.ListAsync();

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            var err = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault();
            return new JsonResult(Result.Error(err ?? "Dữ liệu không hợp lệ."));
        }

        if (Id is Guid g && g != Guid.Empty)
        {
            await _svc.UpdateAsync(g, new UpdateCustomerSourceDto(Input.Name, Input.SortOrder));
        }
        else
        {
            await _svc.CreateAsync(new CreateCustomerSourceDto(Input.Name, Input.SortOrder));
        }

        return new JsonResult(Result.Success("Đã lưu nguồn khách."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá nguồn khách.";
        return RedirectToPage();
    }
}
