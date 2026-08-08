using Microsoft.Extensions.Options;
using TourKit.Ai;
using TourKit.Ai.Abstractions;

namespace TourKit.Api.Ai;

/// <summary>
/// Chấm điểm một bản ghi: lấy hồ sơ từ <see cref="AiRecordSheet"/> rồi gọi
/// <see cref="AiReviewService"/> với bộ tiêu chí trong ai-scoring.json.
/// </summary>
public sealed class AiReviewer(
    AiChatClientFactory factory,
    AiUsageGuard usage,
    IOptionsMonitor<AiScoringOptions> scoring,
    AiRecordSheet sheets,
    ILoggerFactory loggers)
{

    /// <summary>Tính năng chấm điểm có đang bật và dùng được không.</summary>
    public bool IsAvailable => factory.For(AiFeatures.Scoring) is not null;

    /// <summary>
    /// Chấm điểm một bản ghi. Trả về thông điệp lỗi tiếng Việt ở <c>Error</c> khi không làm được —
    /// nơi gọi chỉ cần hiện nguyên văn, không phải tự nghĩ câu.
    /// </summary>
    public async Task<(AiReview? Review, string? Error)> ReviewAsync(
        string entityName, string entityId, Guid userId, CancellationToken ct)
    {
        var resolved = factory.For(AiFeatures.Scoring);
        if (resolved is null)
        {
            return (null, "Tính năng đánh giá đang tắt. Bật Ai:Features:Scoring trong cấu hình để dùng.");
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

        // Luật chấm điểm nằm ở ai-scoring.json, không phải appsettings. Không khai bộ tiêu chí cho
        // loại này thì KHÔNG chấm — thà nói thẳng còn hơn chấm bằng bộ tiêu chí của loại khác rồi cho
        // ra một con số vô nghĩa.
        var rules = scoring.CurrentValue;
        var profile = rules.For(entityName);
        if (profile is null)
        {
            return (null, $"Chưa khai bộ tiêu chí chấm điểm cho loại này (AiScoring:Profiles:{entityName} trong ai-scoring.json).");
        }

        var service = new AiReviewService(
            client, settings, profile, [.. rules.Bands], loggers.CreateLogger<AiReviewService>());
        var review = await service.ReviewAsync(sheet, ct).ConfigureAwait(false);

        // Chấm điểm không đi qua vòng lặp công cụ nên không có số token trả về theo lượt; trừ tạm
        // theo độ dài hồ sơ để một người bấm nút liên tục vẫn chạm hạn mức.
        usage.Record(userId, sheet.Length / 3);

        return review is null
            ? (null, "Trợ lý chưa đưa ra được nhận định rõ ràng cho bản ghi này. Bạn thử lại sau ít phút nhé.")
            : (review, null);
    }
}
