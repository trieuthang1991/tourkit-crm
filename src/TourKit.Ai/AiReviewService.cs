using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using TourKit.Ai.Abstractions;

namespace TourKit.Ai;

/// <summary>Điểm của MỘT tiêu chí, kèm lý do.</summary>
/// <param name="Key">Mã tiêu chí trong cấu hình.</param>
/// <param name="Label">Nhãn tiếng Việt.</param>
/// <param name="Weight">Trọng số dùng để tính điểm tổng.</param>
/// <param name="Score">Điểm 0–100 của riêng tiêu chí này.</param>
/// <param name="Note">Một câu giải thích vì sao được/mất điểm.</param>
public sealed record AiCriterionScore(string Key, string Label, int Weight, int Score, string Note);

/// <summary>Nhận định của AI về một bản ghi nghiệp vụ.</summary>
/// <param name="Score">Điểm tổng 0–100, do HỆ THỐNG tính theo trọng số — không phải model tự nghĩ ra.</param>
/// <param name="Band">Nhóm đọc nhanh, lấy từ thang trong cấu hình.</param>
/// <param name="Summary">Một đến hai câu tóm tắt.</param>
/// <param name="Criteria">Điểm từng tiêu chí — phần cho người dùng thấy điểm đến từ đâu.</param>
/// <param name="Risks">Rủi ro hoặc chỗ còn thiếu thông tin.</param>
/// <param name="NextActions">Việc nên làm tiếp.</param>
public sealed record AiReview(
    int Score,
    string Band,
    string Summary,
    IReadOnlyList<AiCriterionScore> Criteria,
    IReadOnlyList<string> Risks,
    IReadOnlyList<string> NextActions);

/// <summary>
/// Chấm điểm một bản ghi theo BỘ TIÊU CHÍ khai trong cấu hình.
///
/// Model chỉ chấm từng tiêu chí; điểm tổng do đây tính theo trọng số. Để model tự phán một con số thì
/// điểm không giải thích được, không ổn định giữa hai lần chạy, và muốn đổi cách chấm phải sửa mã
/// nguồn. Tách ra thế này thì thêm tiêu chí hay đổi trọng số chỉ là sửa JSON.
/// </summary>
public sealed class AiReviewService(
    IChatClient client,
    AiChatSettings settings,
    AiScoringProfile profile,
    IReadOnlyList<AiScoreBand> bands,
    ILogger<AiReviewService> logger)
{
    /// <summary>Thang xếp nhóm mặc định khi cấu hình không khai.</summary>
    public static IReadOnlyList<AiScoreBand> DefaultBands { get; } =
        [new(80, "Nóng"), new(50, "Ấm"), new(0, "Nguội")];

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Phần KHUNG của prompt — vai trò và định dạng trả về. Cố ý KHÔNG cho sửa từ cấu hình: sửa hỏng
    /// định dạng thì hệ thống không đọc nổi câu trả lời và tính năng chết hẳn. Phần nghiệp vụ (tiêu
    /// chí, trọng số, cách chấm) mới là phần đọc từ JSON.
    /// </summary>
    private const string Frame = """
        Bạn là trợ lý phân tích kinh doanh của một công ty lữ hành Việt Nam. Nhiệm vụ: đọc hồ sơ một
        bản ghi rồi CHẤM ĐIỂM TỪNG TIÊU CHÍ được liệt kê bên dưới.

        # Nguyên tắc
        - Chỉ dựa vào dữ liệu trong hồ sơ. KHÔNG suy đoán thông tin không có ở đó.
        - Tiêu chí nào hồ sơ không có dữ liệu để đánh giá thì cho điểm THẤP và nói rõ là thiếu dữ
          liệu — đừng cho điểm trung bình cho an toàn.
        - Mỗi tiêu chí chấm 0–100 độc lập với nhau. TUYỆT ĐỐI không tự tính điểm tổng.
        - Viết tiếng Việt, ngắn, cụ thể. Việc nên làm phải làm được ngay hôm nay (gọi ai, hỏi gì,
          gửi gì), không phải lời khuyên chung chung.

        # Định dạng trả về
        Chỉ trả về JSON, không lời dẫn, không bọc trong khối mã. Mảng criteria phải có ĐỦ và ĐÚNG các
        key được liệt kê:
        {"criteria":[{"key":"...","score":0,"note":"..."}],"summary":"...","risks":["..."],"nextActions":["..."]}
        """;

    /// <summary>Chấm một hồ sơ. Trả <c>null</c> khi model trả về thứ không đọc được.</summary>
    public async Task<AiReview?> ReviewAsync(string factSheet, CancellationToken ct)
    {
        var options = new ChatOptions
        {
            ModelId = settings.Model,
            MaxOutputTokens = settings.MaxOutputTokens,
            Temperature = settings.Temperature is null ? null : (float)settings.Temperature.Value,
            ResponseFormat = ChatResponseFormat.Json,
        };

        var response = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.System, BuildSystemPrompt()), new ChatMessage(ChatRole.User, factSheet)],
            options,
            ct).ConfigureAwait(false);

        var review = Combine(response.Text, profile, bands);
        if (review is null)
        {
            logger.LogWarning("Không đọc được nhận định AI. Model trả về: {Text}", response.Text);
        }

        return review;
    }

    /// <summary>Ghép khung cố định với bộ tiêu chí trong cấu hình.</summary>
    private string BuildSystemPrompt()
    {
        var sb = new StringBuilder(Frame).Append("\n\n# Các tiêu chí phải chấm\n");
        foreach (var c in profile.Criteria)
        {
            sb.Append("- key \"").Append(c.Key).Append("\" — ").Append(c.Label)
              .Append(" (trọng số ").Append(c.Weight.ToString(CultureInfo.InvariantCulture)).Append("%): ")
              .Append(c.Guide).Append('\n');
        }

        return sb.ToString();
    }

    /// <summary>
    /// Đọc JSON model trả về rồi TÍNH điểm tổng theo trọng số.
    ///
    /// Tách thành hàm thuần để kiểm thử được toàn bộ phần dễ vỡ mà không cần gọi model thật.
    /// </summary>
    public static AiReview? Combine(string? text, AiScoringProfile profile, IReadOnlyList<AiScoreBand>? bands)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var raw = ParsePayload(text);
        if (raw is null || string.IsNullOrWhiteSpace(raw.Summary))
        {
            return null;
        }

        var byKey = (raw.Criteria ?? [])
            .Where(c => !string.IsNullOrWhiteSpace(c.Key))
            .GroupBy(c => c.Key!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var scored = new List<AiCriterionScore>(profile.Criteria.Count);
        foreach (var c in profile.Criteria)
        {
            // Model bỏ sót tiêu chí thì tính 0 và nói rõ, KHÔNG lặng lẽ bỏ tiêu chí ra khỏi mẫu số —
            // bỏ ra thì điểm tổng tự động cao lên đúng vào lúc model làm việc kém nhất.
            var found = byKey.GetValueOrDefault(c.Key);
            scored.Add(new AiCriterionScore(
                c.Key,
                c.Label,
                c.Weight,
                found is null ? 0 : Math.Clamp(found.Score, 0, 100),
                found?.Note?.Trim() is { Length: > 0 } note ? note : "Model không chấm tiêu chí này."));
        }

        var weight = scored.Sum(s => s.Weight);
        var total = weight <= 0 ? 0 : (int)Math.Round(scored.Sum(s => (double)s.Score * s.Weight) / weight);

        return new AiReview(
            total,
            BandOf(total, bands),
            raw.Summary.Trim(),
            scored,
            Clean(raw.Risks),
            Clean(raw.NextActions));
    }

    /// <summary>Nhãn của một điểm số theo thang cấu hình; bậc cao nhất phủ được thì lấy bậc đó.</summary>
    public static string BandOf(int score, IReadOnlyList<AiScoreBand>? bands)
    {
        var scale = bands is null || bands.Count == 0 ? DefaultBands : bands;

        return scale.Where(b => score >= b.Min)
            .OrderByDescending(b => b.Min)
            .Select(b => b.Label)
            .FirstOrDefault() ?? "";
    }

    /// <summary>Model đôi khi vẫn bọc trong ```json dù prompt đã cấm — cắt lấy phần giữa hai ngoặc nhọn.</summary>
    private static Payload? ParsePayload(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var start = text.IndexOf('{', StringComparison.Ordinal);
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Payload>(text[start..(end + 1)], Json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static IReadOnlyList<string> Clean(IReadOnlyList<string>? items) =>
        items is null ? [] : [.. items.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).Take(8)];

    private sealed record CriterionPayload(string? Key, int Score, string? Note);

    private sealed record Payload(
        IReadOnlyList<CriterionPayload>? Criteria,
        string? Summary,
        IReadOnlyList<string>? Risks,
        IReadOnlyList<string>? NextActions);
}
