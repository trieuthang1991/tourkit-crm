using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.Content;

namespace TourKit.Api.Pages.PostCategories;

[Authorize(Policy = "post.view")]
public class IndexModel : PageModel
{
    private readonly IPostCategoryService _svc;
    public IndexModel(IPostCategoryService svc) => _svc = svc;

    public IReadOnlyList<PostCategoryDto> Items { get; private set; } = [];
    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập tên")] public string Name { get; set; } = "";
        [Required(ErrorMessage = "Bắt buộc nhập slug")] public string Slug { get; set; } = "";
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
            await _svc.UpdateAsync(g, new UpdatePostCategoryDto(Input.Name, Input.Slug, Input.SortOrder));
        }
        else
        {
            await _svc.CreateAsync(new CreatePostCategoryDto(Input.Name, Input.Slug, Input.SortOrder));
        }

        return new JsonResult(Result.Success("Đã lưu chuyên mục."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá chuyên mục.";
        return RedirectToPage();
    }
}
