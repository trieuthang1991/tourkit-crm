using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Comments;
using TourKit.Api.Services;
using TourKit.Api.Web;
using TourKit.Application.Collaboration;
using TourKit.Application.Common;

namespace TourKit.Api.Pages.Comments;

/// <summary>
/// Handler JSON cho luồng bình luận. Trang KHÔNG có giao diện — luồng bình luận là một mảnh gắn vào
/// nhiều màn chi tiết khác nhau (cơ hội, khách hàng, đơn hàng) nên phải có một endpoint dùng chung.
///
/// Chỉ đòi [Authorize] ở cửa trang; quyền thật kiểm ở <see cref="Resolve"/> theo ĐÚNG loại bản ghi
/// được yêu cầu. Đặt một policy cố định ở đây là sai: mỗi loại bản ghi có mã quyền khác nhau.
/// </summary>
[Authorize]
public class IndexModel(IEntityCommentService comments, UserDirectory directory) : PageModel
{
    public async Task<IActionResult> OnGetAsync(string entityName, string entityId, int take = 50)
    {
        if (Resolve(entityName) is not { } entry)
        {
            return Denied(entityName);
        }

        var items = await comments.ListAsync(entityName, entityId, take);
        var total = await comments.CountAsync(entityName, entityId);

        // Trả kèm TÊN người được nhắc để giao diện tô đúng những chữ @ là người thật.
        // Nếu để giao diện tự dò chữ "@..." trong nội dung thì "@giá tốt" cũng bị tô như một cái tên.
        var names = items.Any(c => c.MentionedUserIds.Count > 0)
            ? await directory.NamesAsync()
            : new Dictionary<Guid, string>();

        return new JsonResult(new
        {
            label = entry.Label,
            total,
            items = items.Select(c => new
            {
                id = c.Id,
                author = c.AuthorName,
                content = c.Content,
                createdAt = c.CreatedAt,
                canDelete = c.CanDelete,
                mentions = c.MentionedUserIds
                    .Select(u => names.GetValueOrDefault(u))
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToList(),
            }),
        });
    }

    /// <summary>
    /// Danh bạ nhân viên cho ô chọn khi gõ "@". Đọc qua <see cref="UserDirectory"/> (cache 60 giây,
    /// tách theo tenant) chứ không tra thẳng bảng Users — gõ mỗi ký tự mà bắn một truy vấn thì
    /// một câu bình luận có thể sinh ra vài chục lượt nạp cả bảng.
    /// </summary>
    public async Task<IActionResult> OnGetPeopleAsync()
    {
        var people = await directory.ListAsync();

        return new JsonResult(people
            .Where(u => u.IsActive)
            .OrderBy(u => u.FullName, StringComparer.CurrentCulture)
            .Select(u => new { id = u.Id, name = u.FullName, dept = u.DepartmentName })
            .ToList());
    }

    public async Task<IActionResult> OnPostAsync(string entityName, string entityId, string content, string? mentions)
    {
        if (Resolve(entityName) is not { } entry)
        {
            return Denied(entityName);
        }

        try
        {
            var dto = await comments.CreateAsync(new CreateEntityCommentDto(
                entityName,
                entityId,
                content,
                ParseMentions(mentions),
                CommentableEntities.LinkFor(entry, entityId),
                entry.Label));

            return new JsonResult(Result.Success(null, new
            {
                id = dto.Id,
                author = dto.AuthorName,
                content = dto.Content,
                createdAt = dto.CreatedAt,
                canDelete = dto.CanDelete,
            }));
        }
        catch (AppException ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }
    }

    /// <summary>
    /// Xoá không nhận entityName: service chỉ cho TÁC GIẢ xoá, và muốn biết id bình luận thì phải
    /// đọc được luồng — mà đọc luồng đã qua kiểm tra quyền ở OnGetAsync.
    /// </summary>
    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            await comments.DeleteAsync(id);
            return new JsonResult(Result.Success("Đã xoá bình luận."));
        }
        catch (AppException ex)
        {
            return new JsonResult(Result.Error(ex.Message));
        }
    }

    /// <summary>
    /// Tra danh sách trắng RỒI kiểm quyền xem bản ghi cha. Trả null nếu một trong hai không đạt —
    /// cả hai nhánh phải trả về cùng một thông điệp để không lộ loại bản ghi nào có tồn tại.
    /// </summary>
    private CommentableEntities.Entry? Resolve(string entityName)
    {
        var entry = CommentableEntities.Find(entityName);
        if (entry is null)
        {
            return null;
        }

        return User.HasClaim("perm", entry.ViewPermission) ? entry : null;
    }

    private JsonResult Denied(string entityName)
    {
        HttpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
        return new JsonResult(Result.Error("Bạn không có quyền xem bình luận của mục này."));
    }

    /// <summary>Danh sách @nhắc từ giao diện: chuỗi Guid ngăn bằng dấu phẩy. Guid rác bị bỏ im lặng —
    /// service còn lọc lại lần nữa theo user có thật.</summary>
    private static List<Guid> ParseMentions(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return [];
        }

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .ToList();
    }
}
