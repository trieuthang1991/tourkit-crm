using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Ai;
using TourKit.Api.Web;

namespace TourKit.Api.Pages.Ai;

/// <summary>
/// Handler JSON của trợ lý. Chỉ cần <c>[Authorize]</c>: phân quyền THẬT nằm ở chỗ lọc danh sách công
/// cụ theo claim <c>perm</c> của người hỏi, nên người ít quyền vẫn mở được trợ lý nhưng nó không tra
/// được những mục họ không được xem.
/// </summary>
[Authorize]
public class ChatModel(AiAssistant assistant, ILogger<ChatModel> logger) : PageModel
{
    private const int MaxQuestionLength = 2000;

    /// <summary>Trang không có giao diện riêng — vào thẳng thì trả 404.</summary>
    public IActionResult OnGet() => NotFound();

    /// <summary>Trợ lý có đang bật không — giao diện dùng để ẩn/hiện nút mở.</summary>
    public IActionResult OnGetStatus() => new JsonResult(new { available = assistant.IsAvailable });

    /// <summary>Hỏi một câu. Trả về Result { isSuccess, message, data:{ text, blocks } }.</summary>
    public async Task<IActionResult> OnPostAskAsync(string? question, CancellationToken ct)
    {
        var q = question?.Trim();
        if (string.IsNullOrEmpty(q))
        {
            return new JsonResult(Result.Error("Bạn chưa nhập câu hỏi."));
        }

        if (q.Length > MaxQuestionLength)
        {
            return new JsonResult(Result.Error($"Câu hỏi dài quá ({q.Length} ký tự), tối đa {MaxQuestionLength}."));
        }

        try
        {
            var answer = await assistant.AskAsync(q, User, ct);
            if (answer is null)
            {
                return new JsonResult(Result.Error("Trợ lý đang tắt. Liên hệ quản trị viên để bật trong cấu hình."));
            }

            return new JsonResult(Result.Success(null, new
            {
                text = answer.Text,
                blocks = answer.Blocks.Select(b => new { tool = b.Tool, data = b.Data, linkUrl = b.LinkUrl }),
            }));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
#pragma warning disable CA1031 // AI là lớp phụ trợ: hỏng thì báo nhẹ nhàng, tuyệt đối không để lộ lỗi hãng ra màn hình.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            logger.LogError(ex, "Trợ lý lỗi khi trả lời câu hỏi.");
            return new JsonResult(Result.Error("Trợ lý đang bận hoặc gặp sự cố, bạn thử lại sau ít phút nhé."));
        }
    }
}
