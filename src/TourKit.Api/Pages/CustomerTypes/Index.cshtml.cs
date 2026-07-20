using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Api.Pages.CustomerTypes;

[Authorize(Policy = "customertype.view")]
public class IndexModel : PageModel
{
    private readonly ICustomerTypeService _svc;
    public IndexModel(ICustomerTypeService svc) => _svc = svc;

    public IReadOnlyList<CustomerTypeDto> Items { get; private set; } = [];
    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        public int Code { get; set; }
        [Required(ErrorMessage = "Bắt buộc nhập tên")] public string Name { get; set; } = "";
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
            await _svc.UpdateAsync(g, new UpdateCustomerTypeDto(Input.Code, Input.Name, Input.SortOrder));
        }
        else
        {
            await _svc.CreateAsync(new CreateCustomerTypeDto(Input.Code, Input.Name, Input.SortOrder));
        }

        return new JsonResult(Result.Success("Đã lưu loại khách hàng."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá loại khách hàng.";
        return RedirectToPage();
    }
}
