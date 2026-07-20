using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Api.Pages.MarketTypes;

[Authorize(Policy = "market.view")]
public class IndexModel : PageModel
{
    private readonly IMarketTypeService _svc;
    public IndexModel(IMarketTypeService svc) => _svc = svc;

    public IReadOnlyList<MarketTypeDto> Items { get; private set; } = [];
    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập tên")] public string Name { get; set; } = "";
        public Guid? ParentId { get; set; }
        public int SortOrder { get; set; }
    }

    public async Task OnGetAsync() => Items = await _svc.ListAsync();

    public string? ParentName(Guid? parentId) =>
        parentId is null ? null : Items.FirstOrDefault(x => x.Id == parentId)?.Name;

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        if (Id is Guid g && g != Guid.Empty)
        {
            await _svc.UpdateAsync(g, new UpdateMarketTypeDto(Input.Name, Input.ParentId, Input.SortOrder));
        }
        else
        {
            await _svc.CreateAsync(new CreateMarketTypeDto(Input.Name, Input.ParentId, Input.SortOrder));
        }

        return new JsonResult(Result.Success("Đã lưu thị trường."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá thị trường.";
        return RedirectToPage();
    }
}
