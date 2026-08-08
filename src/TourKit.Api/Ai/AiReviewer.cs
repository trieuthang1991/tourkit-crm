using System.Globalization;
using Microsoft.Extensions.Options;
using System.Text;
using TourKit.Ai;
using TourKit.Ai.Abstractions;
using TourKit.Application.Collaboration;
using TourKit.Application.Crm;
using TourKit.Application.Customers;

namespace TourKit.Api.Ai;

/// <summary>
/// Dựng hồ sơ cho AI chấm điểm, rồi gọi <see cref="AiReviewService"/>.
///
/// Việc dựng hồ sơ nằm ở tầng Api chứ không ở lõi AI vì nó phải gọi service nghiệp vụ — và nhờ vậy
/// bộ lọc theo công ty, ẩn bản ghi đã xoá của những service đó tự động áp dụng. AI không bao giờ nhìn
/// thấy dữ liệu mà người đang hỏi không được xem.
///
/// Luồng trao đổi (bình luận) là phần có giá trị nhất trong hồ sơ: bản ghi chỉ có vài trường cố định,
/// còn diễn biến thật của một cơ hội nằm trong lời nhân viên ghi lại.
/// </summary>
public sealed class AiReviewer(
    AiChatClientFactory factory,
    AiUsageGuard usage,
    IOptionsMonitor<AiScoringOptions> scoring,
    ILeadService leads,
    ICustomerService customers,
    IEntityCommentService comments,
    ILoggerFactory loggers)
{
    private const int MaxComments = 30;

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

        var sheet = await BuildSheetAsync(entityName, entityId, ct).ConfigureAwait(false);
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

    private async Task<string?> BuildSheetAsync(string entityName, string entityId, CancellationToken ct)
    {
        if (!Guid.TryParse(entityId, out var id))
        {
            return null;
        }

        var sb = new StringBuilder();

        switch (entityName)
        {
            case "Lead":
                var lead = await leads.GetAsync(id).ConfigureAwait(false);
                sb.Append("HỒ SƠ CƠ HỘI BÁN HÀNG\n")
                  .Append("- Tên khách: ").Append(lead.FullName).Append('\n')
                  .Append("- Nguồn: ").Append(Or(lead.Source)).Append('\n')
                  .Append("- Trạng thái: ").Append(lead.Status).Append('\n')
                  .Append("- Có số điện thoại: ").Append(Yes(lead.Phone)).Append('\n')
                  .Append("- Có email: ").Append(Yes(lead.Email)).Append('\n')
                  .Append("- Đã chuyển thành khách hàng: ").Append(lead.ConvertedCustomerId is null ? "chưa" : "rồi").Append('\n');
                break;

            case "Customer":
                var customer = await customers.GetAsync(id).ConfigureAwait(false);
                sb.Append("HỒ SƠ KHÁCH HÀNG\n")
                  .Append("- Tên: ").Append(customer.FullName).Append('\n')
                  .Append("- Nguồn: ").Append(Or(customer.Source)).Append('\n')
                  .Append("- Nhóm thị trường: ").Append(Or(customer.MarketGroup)).Append('\n')
                  .Append("- Nhu cầu ban đầu: ").Append(Or(customer.InitialNeed)).Append('\n')
                  .Append("- Thành phố: ").Append(Or(customer.City)).Append('\n')
                  .Append("- Số dư tạm: ").Append(customer.TempBalance.ToString("N0", CultureInfo.InvariantCulture).Replace(',', '.')).Append(" đồng\n")
                  .Append("- Phân khúc: ").Append(Join(customer.Segments)).Append('\n')
                  .Append("- Nhãn: ").Append(Join(customer.Tags)).Append('\n')
                  .Append("- Ghi chú: ").Append(Or(customer.Note)).Append('\n');
                break;

            default:
                return null;
        }

        var thread = await comments.ListAsync(entityName, entityId, MaxComments).ConfigureAwait(false);
        sb.Append("\nDIỄN BIẾN TRAO ĐỔI NỘI BỘ (mới nhất trước, tối đa ")
          .Append(MaxComments.ToString(CultureInfo.InvariantCulture)).Append(" dòng):\n");

        if (thread.Count == 0)
        {
            sb.Append("(chưa có trao đổi nào — đây là một thiếu sót dữ liệu, hãy tính đến khi cho điểm)\n");
        }
        else
        {
            foreach (var c in thread)
            {
                sb.Append("- [").Append(c.CreatedAt.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)).Append("] ")
                  .Append(c.AuthorName).Append(": ").Append(c.Content.ReplaceLineEndings(" ")).Append('\n');
            }
        }

        return sb.ToString();
    }

    private static string Or(string? value) => string.IsNullOrWhiteSpace(value) ? "(chưa có)" : value;

    private static string Yes(string? value) => string.IsNullOrWhiteSpace(value) ? "không" : "có";

    private static string Join(IReadOnlyList<string> values) => values.Count == 0 ? "(chưa có)" : string.Join(", ", values);
}
