using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Web;
using TourKit.Application.Content;

namespace TourKit.Api.Pages.Posts;

[Authorize(Policy = "post.view")]
public class IndexModel : PageModel
{
    private readonly IPostService _svc;
    private readonly IPostCategoryService _cats;
    public IndexModel(IPostService svc, IPostCategoryService cats)
    {
        _svc = svc;
        _cats = cats;
    }

    public IReadOnlyList<PostDto> Items { get; private set; } = [];
    public IReadOnlyList<PostCategoryDto> Categories { get; private set; } = [];

    [BindProperty] public Guid? Id { get; set; }
    [BindProperty] public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Required(ErrorMessage = "Bắt buộc nhập tiêu đề")] public string Title { get; set; } = "";
        [Required(ErrorMessage = "Bắt buộc nhập slug")] public string Slug { get; set; } = "";
        public string? Summary { get; set; }
        public string Body { get; set; } = "";
        public Guid? CategoryId { get; set; }
        public int Status { get; set; }
        public int LikeCount { get; set; }
    }

    public static string StatusLabel(int s) => s switch { 1 => "Xuất bản", 2 => "Lưu trữ", _ => "Nháp" };
    public static string StatusColor(int s) => s switch { 1 => "success", 2 => "secondary", _ => "warning" };

    public async Task OnGetAsync()
    {
        Items = (await _svc.ListAsync(1, 500, null, null)).Items;
        Categories = await _cats.ListAsync();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(Result.Error(ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).FirstOrDefault() ?? "Dữ liệu không hợp lệ."));
        }

        if (Id is Guid g && g != Guid.Empty)
        {
            await _svc.UpdateAsync(g, new UpdatePostDto(Input.Title, Input.Slug, Input.Summary, Input.Body, Input.CategoryId, Input.Status, Input.LikeCount));
        }
        else
        {
            await _svc.CreateAsync(new CreatePostDto(Input.Title, Input.Slug, Input.Summary, Input.Body, Input.CategoryId, Input.Status));
        }

        return new JsonResult(Result.Success("Đã lưu bài viết."));
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        await _svc.DeleteAsync(id);
        TempData["ok"] = "Đã xoá bài viết.";
        return RedirectToPage();
    }
}
