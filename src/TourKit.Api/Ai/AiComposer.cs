using TourKit.Ai;
using TourKit.Ai.Abstractions;

namespace TourKit.Api.Ai;

/// <summary>
/// Hai tính năng sinh văn bản từ một bản ghi: TÓM TẮT diễn biến và SOẠN tin nhắn cho khách.
///
/// Gộp chung một lớp vì chúng khác nhau đúng một thứ: prompt. Tách thành hai lớp thì phần lấy cấu
/// hình, chặn hạn mức, dựng hồ sơ và trừ token bị nhân đôi — và hai bản sao đó sẽ lệch nhau.
/// </summary>
public sealed class AiComposer(
    AiChatClientFactory factory,
    AiUsageGuard usage,
    AiRecordSheet sheets,
    ILoggerFactory loggers)
{
    /// <summary>Tính năng tóm tắt có đang dùng được không.</summary>
    public bool CanSummarize => factory.For(AiFeatures.Summarize) is not null;

    /// <summary>Tính năng soạn tin có đang dùng được không.</summary>
    public bool CanDraft => factory.For(AiFeatures.Draft) is not null;

    /// <summary>Tóm tắt diễn biến của một bản ghi.</summary>
    public Task<(string? Text, string? Error)> SummarizeAsync(
        string entityName, string entityId, Guid userId, CancellationToken ct) =>
        RunAsync(AiFeatures.Summarize, AiWriterService.SummaryPrompt, entityName, entityId, userId, ct);

    /// <summary>Soạn một tin nhắn gửi khách, dựa trên hồ sơ và diễn biến trao đổi.</summary>
    public Task<(string? Text, string? Error)> DraftAsync(
        string entityName, string entityId, Guid userId, CancellationToken ct) =>
        RunAsync(AiFeatures.Draft, AiWriterService.DraftPrompt, entityName, entityId, userId, ct);

    private async Task<(string? Text, string? Error)> RunAsync(
        string feature, string prompt, string entityName, string entityId, Guid userId, CancellationToken ct)
    {
        var resolved = factory.For(feature);
        if (resolved is null)
        {
            return (null, $"Tính năng \"{AiFeatures.Label(feature)}\" đang tắt. Bật Ai:Features:{feature} trong cấu hình để dùng.");
        }

        if (usage.Reject(userId) is { } refusal)
        {
            return (null, refusal);
        }

        var sheet = await sheets.BuildAsync(entityName, entityId).ConfigureAwait(false);
        if (sheet is null)
        {
            return (null, "Không đọc được bản ghi này.");
        }

        var (client, config) = resolved.Value;
        var settings = new AiChatSettings(
            config.Model,
            config.Settings.MaxOutputTokens,
            config.Settings.MaxToolRounds,
            config.Settings.Temperature);

        var writer = new AiWriterService(client, settings, loggers.CreateLogger<AiWriterService>());
        var text = await writer.WriteAsync(prompt, sheet, ct).ConfigureAwait(false);

        // Không đi qua vòng lặp công cụ nên không có số token trả về theo lượt; trừ tạm theo độ dài
        // hồ sơ để người bấm nút liên tục vẫn chạm hạn mức.
        usage.Record(userId, sheet.Length / 3);

        return text is null
            ? (null, "Trợ lý chưa soạn được nội dung cho bản ghi này. Bạn thử lại sau ít phút nhé.")
            : (text, null);
    }
}
