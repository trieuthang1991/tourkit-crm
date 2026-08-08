using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Ai;
using TourKit.Api.Comments;
using TourKit.Api.Web;

namespace TourKit.Api.Pages.Ai;

/// <summary>
/// Handler JSON của tính năng đánh giá.
///
/// Dùng lại <see cref="CommentableEntities"/> làm danh sách trắng: chỉ loại bản ghi có trong đó mới
/// chấm được, và phải có đúng quyền XEM bản ghi đó. Dùng chung một bảng với bình luận là có chủ ý —
/// hai danh sách riêng sớm muộn sẽ lệch nhau và cho ra tổ hợp "không xem được bản ghi nhưng chấm
/// điểm được nó", mà nhận định thì kể lại chính nội dung bản ghi.
/// </summary>
[Authorize]
public class ReviewModel(AiReviewer reviewer, ILogger<ReviewModel> logger) : PageModel
{
    /// <summary>Trang không có giao diện riêng.</summary>
    public IActionResult OnGet() => NotFound();

    /// <summary>Tính năng có bật không — giao diện dùng để ẩn/hiện nút.</summary>
    public IActionResult OnGetStatus() => new JsonResult(new { available = reviewer.IsAvailable });

    /// <summary>Chấm điểm một bản ghi.</summary>
    public async Task<IActionResult> OnPostRunAsync(string? entity, string? id, CancellationToken ct)
    {
        var entry = CommentableEntities.Find(entity);
        if (entry is null || string.IsNullOrWhiteSpace(id))
        {
            return new JsonResult(Result.Error("Không đánh giá được loại bản ghi này."));
        }

        if (!User.HasClaim("perm", entry.ViewPermission))
        {
            return new JsonResult(Result.Error("Bạn không có quyền xem bản ghi này."));
        }

        var userId = ReadUserId(User);
        if (userId is null)
        {
            return new JsonResult(Result.Error("Phiên đăng nhập có vấn đề. Bạn đăng nhập lại nhé."));
        }

        try
        {
            var (review, error) = await reviewer.ReviewAsync(entity!, id!, userId.Value, ct);
            if (review is null)
            {
                return new JsonResult(Result.Error(error ?? "Chưa đánh giá được."));
            }

            return new JsonResult(Result.Success(null, new
            {
                score = review.Score,
                band = review.Band,
                summary = review.Summary,
                criteria = review.Criteria.Select(c => new { c.Label, c.Weight, c.Score, c.Note }),
                risks = review.Risks,
                nextActions = review.NextActions,
            }));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
#pragma warning disable CA1031 // AI là lớp phụ trợ: hỏng thì báo nhẹ, không để lộ lỗi hãng ra màn hình.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            logger.LogError(ex, "Lỗi khi đánh giá {Entity}.", entity);
            return new JsonResult(Result.Error("Trợ lý đang bận hoặc gặp sự cố, bạn thử lại sau ít phút nhé."));
        }
    }

    private static Guid? ReadUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
