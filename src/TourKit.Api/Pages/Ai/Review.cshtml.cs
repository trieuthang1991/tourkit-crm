using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TourKit.Api.Ai;
using TourKit.Api.Web;

namespace TourKit.Api.Pages.Ai;

/// <summary>
/// Handler JSON của tính năng đánh giá.
///
/// Dùng lại <see cref="AiRecordAccess"/> làm danh sách trắng: chỉ loại bản ghi có trong đó mới
/// chấm được, và phải có đúng quyền XEM bản ghi đó. Dùng chung một bảng với bình luận là có chủ ý —
/// hai danh sách riêng sớm muộn sẽ lệch nhau và cho ra tổ hợp "không xem được bản ghi nhưng chấm
/// điểm được nó", mà nhận định thì kể lại chính nội dung bản ghi.
/// </summary>
[Authorize]
public class ReviewModel(
    AiReviewer reviewer,
    AiComposer composer,
    TourKit.Application.Ai.IAiInsightStore store,
    ILogger<ReviewModel> logger) : PageModel
{
    /// <summary>Kết quả gần nhất của cả ba việc — màn hình gọi khi mở hồ sơ, để không phải chấm lại.</summary>
    public async Task<IActionResult> OnGetLatestAsync(string? entity, string? id, CancellationToken ct)
    {
        var guard = AiRecordAccess.Check(User, entity, id);
        if (guard.Error is not null)
        {
            return new JsonResult(Result.Error(guard.Error));
        }

        var items = await store.LatestAsync(entity!, id!, ct);
        return new JsonResult(Result.Success(null, new { items }));
    }

    /// <summary>Lịch sử một loại việc, mới nhất trước — để so điểm lần này với những lần trước.</summary>
    public async Task<IActionResult> OnGetHistoryAsync(string? entity, string? id, string? kind, CancellationToken ct)
    {
        var guard = AiRecordAccess.Check(User, entity, id);
        if (guard.Error is not null)
        {
            return new JsonResult(Result.Error(guard.Error));
        }

        if (string.IsNullOrWhiteSpace(kind))
        {
            return new JsonResult(Result.Error("Thiếu loại kết quả cần xem."));
        }

        var items = await store.HistoryAsync(entity!, id!, kind, 20, ct);
        return new JsonResult(Result.Success(null, new { items }));
    }

    /// <summary>Trang không có giao diện riêng.</summary>
    public IActionResult OnGet() => NotFound();

    /// <summary>Tính năng nào đang bật — giao diện dùng để hiện đúng những nút dùng được.</summary>
    public IActionResult OnGetStatus() => new JsonResult(new
    {
        available = reviewer.IsAvailable || composer.CanSummarize || composer.CanDraft,
        review = reviewer.IsAvailable,
        summary = composer.CanSummarize,
        draft = composer.CanDraft,
    });

    /// <summary>Tóm tắt diễn biến của một bản ghi.</summary>
    public Task<IActionResult> OnPostSummaryAsync(string? entity, string? id, CancellationToken ct) =>
        TextAsync(entity, id, (e, i, u) => composer.SummarizeAsync(e, i, u, ct), "tóm tắt", "Summary", ct);

    /// <summary>Soạn một tin nhắn gửi khách.</summary>
    public Task<IActionResult> OnPostDraftAsync(string? entity, string? id, CancellationToken ct) =>
        TextAsync(entity, id, (e, i, u) => composer.DraftAsync(e, i, u, ct), "soạn tin", "Draft", ct);

    /// <summary>
    /// Khung chung cho hai tính năng sinh văn bản: cùng cách soát danh sách trắng, cùng cách kiểm
    /// quyền, cùng cách nuốt lỗi. Viết hai lần thì hai bản sao đó sẽ lệch nhau ở đúng phần bảo mật.
    /// </summary>
    private async Task<IActionResult> TextAsync(
        string? entity,
        string? id,
        Func<string, string, Guid, Task<(string? Text, string? Error)>> run,
        string what,
        string kind,
        CancellationToken ct)
    {
        var guard = AiRecordAccess.Check(User, entity, id);
        if (guard.Error is not null)
        {
            return new JsonResult(Result.Error(guard.Error));
        }

        try
        {
            var (text, error) = await run(entity!, id!, guard.UserId);
            if (text is null)
            {
                return new JsonResult(Result.Error(error ?? "Chưa làm được."));
            }

            // Lưu NGAY sau khi có kết quả. Trước đây kết quả chỉ đi thẳng ra trình duyệt rồi mất khi
            // tải lại trang — mở hồ sơ hôm sau là trống trơn, muốn xem lại phải chạy lại và trả tiền
            // model thêm một lượt.
            await store.SaveAsync(new TourKit.Application.Ai.SaveAiInsightDto(
                entity!, id!, kind, guard.UserId, Text: text), ct);

            return new JsonResult(Result.Success(null, new { text }));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
#pragma warning disable CA1031 // AI là lớp phụ trợ: hỏng thì báo nhẹ, không để lộ lỗi hãng ra màn hình.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            logger.LogError(ex, "Lỗi khi {What} cho {Entity}.", what, entity);
            return new JsonResult(Result.Error("Trợ lý đang bận hoặc gặp sự cố, bạn thử lại sau ít phút nhé."));
        }
    }

    /// <summary>Chấm điểm một bản ghi.</summary>
    public async Task<IActionResult> OnPostReviewAsync(string? entity, string? id, CancellationToken ct)
    {
        var guard = AiRecordAccess.Check(User, entity, id);
        if (guard.Error is not null)
        {
            return new JsonResult(Result.Error(guard.Error));
        }

        try
        {
            var (review, error) = await reviewer.ReviewAsync(entity!, id!, guard.UserId, ct);
            if (review is null)
            {
                return new JsonResult(Result.Error(error ?? "Chưa đánh giá được."));
            }

            var chiTiet = new
            {
                criteria = review.Criteria.Select(c => new { c.Label, c.Weight, c.Score, c.Note }),
                risks = review.Risks,
                nextActions = review.NextActions,
            };

            // Giữ lại để lần sau mở hồ sơ còn thấy, và để so điểm lần này với những lần trước — chính
            // diễn biến điểm mới là thứ nói lên khách đang ấm lên hay nguội đi.
            await store.SaveAsync(new TourKit.Application.Ai.SaveAiInsightDto(
                entity!, id!, "Review", guard.UserId,
                Score: review.Score, Band: review.Band, Summary: review.Summary,
                DetailJson: System.Text.Json.JsonSerializer.Serialize(chiTiet)), ct);

            return new JsonResult(Result.Success(null, new
            {
                score = review.Score,
                band = review.Band,
                summary = review.Summary,
                chiTiet.criteria,
                chiTiet.risks,
                chiTiet.nextActions,
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
}
