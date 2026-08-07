using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Comments;
using TourKit.Api.Services;
using TourKit.Api.Web;
using TourKit.Application.Admin;
using TourKit.Application.Collaboration;
using TourKit.Application.Common;
using TourKit.Application.Files;

namespace TourKit.Api.Pages.Comments;

/// <summary>
/// Handler JSON cho luồng bình luận. Trang KHÔNG có giao diện — luồng bình luận là một mảnh gắn vào
/// nhiều màn chi tiết khác nhau (cơ hội, khách hàng, đơn hàng) nên phải có một endpoint dùng chung.
///
/// Chỉ đòi [Authorize] ở cửa trang; quyền thật kiểm ở <see cref="Resolve"/> theo ĐÚNG loại bản ghi
/// được yêu cầu. Đặt một policy cố định ở đây là sai: mỗi loại bản ghi có mã quyền khác nhau.
/// </summary>
[Authorize]
public class IndexModel(
    IEntityCommentService comments, UserDirectory directory, IFileUploadService files, IRbacStore rbac) : PageModel
{
    private const long MaxImageBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedImageTypes =
        new(StringComparer.Ordinal) { "image/jpeg", "image/png", "image/webp", "image/gif" };

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
                attachments = c.Attachments.Select(a => new
                {
                    id = a.Id,
                    name = a.FileName,
                    // URL kèm entityName/entityId để endpoint ảnh kiểm lại được quyền của bản ghi cha.
                    url = $"/binh-luan?handler=Image&entityName={Uri.EscapeDataString(entityName)}" +
                          $"&entityId={Uri.EscapeDataString(entityId)}&id={a.Id}",
                }).ToList(),
            }),
        });
    }

    /// <summary>
    /// Danh bạ cho ô chọn khi gõ "@" — CHỈ những người mở được bản ghi này.
    ///
    /// Nhắc người không có quyền xem là làm hại họ: họ nhận thông báo, bấm vào và bị đá về màn đăng
    /// nhập — trông hệt như hết phiên, không ai đoán được là do thiếu quyền.
    ///
    /// Đọc qua <see cref="UserDirectory"/> (cache 60 giây, tách theo tenant) chứ không tra thẳng bảng
    /// Users — gõ mỗi ký tự mà bắn một truy vấn thì một câu bình luận sinh ra vài chục lượt nạp cả bảng.
    /// </summary>
    public async Task<IActionResult> OnGetPeopleAsync(string entityName)
    {
        if (Resolve(entityName) is not { } entry)
        {
            return Denied(entityName);
        }

        var eligible = await EligibleMentionsAsync(entry);
        var people = await directory.ListAsync();

        return new JsonResult(people
            .Where(u => u.IsActive && eligible.Contains(u.Id))
            .OrderBy(u => u.FullName, StringComparer.CurrentCulture)
            .Select(u => new { id = u.Id, name = u.FullName, dept = u.DepartmentName })
            .ToList());
    }

    private async Task<HashSet<Guid>> EligibleMentionsAsync(CommentableEntities.Entry entry) =>
        (await rbac.UserIdsWithPermissionAsync(entry.ViewPermission)).ToHashSet();

    /// <summary>
    /// Tải một ảnh lên TRƯỚC khi gửi bình luận. Trả về id để giao diện gửi kèm lúc bấm Gửi —
    /// nhờ vậy người dùng thấy ảnh xuất hiện ngay khi chọn, không phải chờ tới lúc gửi mới biết hỏng.
    /// </summary>
    public async Task<IActionResult> OnPostUploadAsync(string entityName, string entityId, IFormFile? file)
    {
        if (Resolve(entityName) is null)
        {
            return Denied(entityName);
        }

        if (file is null || file.Length == 0)
        {
            return new JsonResult(Result.Error("Chưa chọn ảnh."));
        }

        if (file.Length > MaxImageBytes)
        {
            return new JsonResult(Result.Error($"Ảnh tối đa {MaxImageBytes / 1024 / 1024} MB."));
        }

        // Chặn theo loại nội dung THẬT, không theo đuôi tên tệp: đổi tên "x.exe" thành "x.png"
        // là chuyện ai cũng làm được.
        var contentType = (file.ContentType ?? "").ToLowerInvariant();
        if (!AllowedImageTypes.Contains(contentType))
        {
            return new JsonResult(Result.Error("Chỉ nhận ảnh JPG, PNG, WEBP hoặc GIF."));
        }

        await using var stream = file.OpenReadStream();
        var saved = await files.SaveAsync(Path.GetFileName(file.FileName), contentType, file.Length, stream);

        return new JsonResult(Result.Success(null, new
        {
            id = saved.Id,
            name = saved.FileName,
            size = saved.Size,
        }));
    }

    /// <summary>
    /// Xem một ảnh đính kèm. Kiểm HAI lớp: quyền xem bản ghi cha, VÀ ảnh phải thật sự thuộc luồng
    /// trao đổi của bản ghi đó. Thiếu lớp thứ hai thì ai có quyền xem một khách hàng bất kỳ cũng đọc
    /// được mọi tệp trong tenant chỉ cần biết id.
    /// </summary>
    public async Task<IActionResult> OnGetImageAsync(string entityName, string entityId, Guid id)
    {
        if (Resolve(entityName) is null)
        {
            return Denied(entityName);
        }

        var thread = await comments.ListAsync(entityName, entityId, 200);
        if (!thread.Any(c => c.Attachments.Any(a => a.Id == id)))
        {
            return Denied(entityName);
        }

        try
        {
            var (meta, content) = await files.OpenAsync(id);
            return File(content, meta.ContentType);
        }
        catch (AppException)
        {
            return NotFound();
        }
    }

    public async Task<IActionResult> OnPostAsync(
        string entityName, string entityId, string content, string? mentions, string? attachments)
    {
        if (Resolve(entityName) is not { } entry)
        {
            return Denied(entityName);
        }

        var eligible = await EligibleMentionsAsync(entry);

        try
        {
            var dto = await comments.CreateAsync(new CreateEntityCommentDto(
                entityName,
                entityId,
                content,
                // Lọc lại ở đây chứ không tin danh sách từ giao diện: request tự chế vẫn có thể
                // nhét id của người không có quyền xem bản ghi.
                ParseGuids(mentions).Where(eligible.Contains).ToList(),
                CommentableEntities.LinkFor(entry, entityId),
                entry.Label,
                ParseGuids(attachments)));

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

    /// <summary>Danh sách id từ giao diện (@nhắc hoặc ảnh): chuỗi Guid ngăn bằng dấu phẩy.
    /// Guid rác bị loại ở đây; service còn kiểm lại lần nữa theo bản ghi có thật.</summary>
    private static List<Guid> ParseGuids(string? raw)
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
