using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TourKit.Api.Pages.Shared;
using TourKit.Api.Web;
using TourKit.Application.Content;

namespace TourKit.Api.Pages.Posts;

// Danh sách bài viết: DataTables SERVER-SIDE (không get-all) + giữ đủ thông tin bản cũ
// (web/src/features/posts/PostsPage.tsx): cột Tiêu đề/Chuyên mục/Trạng thái/Xuất bản/Lượt thích,
// nút Bình luận (modal duyệt/xoá/thêm), Sửa, Xoá.
// IPostService.ListAsync(page, size, categoryId, status) KHÔNG nhận từ khoá → ẩn ô search mặc định
// của DataTables, chỉ đẩy 2 tiêu chí có thật (chuyên mục, trạng thái) xuống service.
[Authorize(Policy = "post.view")]
public class IndexModel : TkListPageModel
{
    private readonly IPostService _svc;
    private readonly IPostCategoryService _cats;
    private readonly IPostCommentService _comments;

    public IndexModel(IPostService svc, IPostCategoryService cats, IPostCommentService comments)
    {
        _svc = svc;
        _cats = cats;
        _comments = comments;
    }

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

    public async Task OnGetAsync() => Categories = await _cats.ListAsync();

    /// <summary>Nguồn DataTables server-side: chỉ trả đúng 1 trang.</summary>
    public async Task<IActionResult> OnGetDataAsync()
    {
        var dt = ParseDataTables();
        var q = Request.Query;
        Guid? categoryId = Guid.TryParse(q["categoryId"], out var c) ? c : null;
        int? status = int.TryParse(q["status"], out var s) ? s : null;

        var result = await _svc.ListAsync(dt.Page, dt.Size, categoryId, status, dt.Keyword);
        var data = result.Items.Select(p => new
        {
            id = p.Id,
            title = p.Title,
            slug = p.Slug,
            summary = p.Summary,
            body = p.Body,
            categoryId = p.CategoryId,
            categoryName = p.CategoryName ?? "—",
            status = p.Status,
            statusLabel = StatusLabel(p.Status),
            statusColor = StatusColor(p.Status),
            publishedAtText = p.PublishedAt?.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) ?? "—",
            likeCount = p.LikeCount,
        });

        return DtJson(dt.Draw, result.Total, result.Total, data);
    }

    /// <summary>Danh sách bình luận của 1 bài viết (modal Bình luận của bản cũ).</summary>
    public async Task<IActionResult> OnGetCommentsAsync(Guid postId)
    {
        var items = await _comments.ListAsync(postId, null);
        return new JsonResult(items.Select(x => new
        {
            id = x.Id,
            authorName = x.AuthorName,
            content = x.Content,
            isApproved = x.IsApproved,
            createdAtText = x.CreatedAt.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture),
        }));
    }

    public async Task<IActionResult> OnPostCommentSaveAsync(Guid postId, string? authorName, string? content, bool isApproved)
    {
        if (string.IsNullOrWhiteSpace(authorName) || string.IsNullOrWhiteSpace(content))
        {
            return new JsonResult(Result.Error("Nhập tên người bình luận và nội dung."));
        }

        await _comments.CreateAsync(postId, new CreatePostCommentDto(authorName.Trim(), content.Trim(), isApproved));
        return new JsonResult(Result.Success("Đã thêm bình luận."));
    }

    public async Task<IActionResult> OnPostCommentApproveAsync(Guid postId, Guid commentId, bool approved)
    {
        await _comments.SetApprovedAsync(postId, commentId, approved);
        return new JsonResult(Result.Success(approved ? "Đã duyệt bình luận." : "Đã bỏ duyệt bình luận."));
    }

    public async Task<IActionResult> OnPostCommentDeleteAsync(Guid postId, Guid commentId)
    {
        await _comments.DeleteAsync(postId, commentId);
        return new JsonResult(Result.Success("Đã xoá bình luận."));
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
