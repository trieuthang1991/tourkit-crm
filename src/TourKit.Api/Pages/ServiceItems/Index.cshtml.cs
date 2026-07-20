using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.Providers;
using TourKit.Application.Providers.Dtos;

namespace TourKit.Api.Pages.ServiceItems;

[Authorize(Policy = "service.view")]
public class IndexModel : PageModel
{
    private readonly IServiceItemService _svc;
    public IndexModel(IServiceItemService svc) => _svc = svc;

    public IReadOnlyList<ServiceItemDto> Items { get; private set; } = [];
    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập mã")] public string Code { get; set; } = "";
        [Required(ErrorMessage = "Bắt buộc nhập tên")] public string Name { get; set; } = "";
        public int Category { get; set; }
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
            await _svc.UpdateAsync(g, new UpdateServiceItemDto(Input.Name, Input.Category, Input.Status));
        }
        else
        {
            await _svc.CreateAsync(new CreateServiceItemDto(Input.Code, Input.Name, Input.Category, Input.Status));
        }

        return new JsonResult(Result.Success("Đã lưu dịch vụ."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá dịch vụ.";
        return RedirectToPage();
    }
}
