using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace TourKit.Ai;

/// <summary>
/// Sinh VĂN BẢN từ một hồ sơ: tóm tắt diễn biến, soạn tin nhắn cho khách.
///
/// Một lượt, không công cụ, không JSON — khác hẳn <see cref="AiChatService"/> (vòng lặp gọi công cụ)
/// và <see cref="AiReviewService"/> (đầu ra có cấu trúc). Ba bài toán ba hình dạng; nhét chung vào
/// một lớp chỉ làm cả ba khó đọc.
///
/// KHÔNG ghi gì vào hệ thống. Kết quả hiện ra để người dùng đọc, sửa, rồi tự quyết định dùng hay
/// không — đúng ràng buộc "AI đọc và soạn sẵn, người bấm xác nhận mới ghi".
/// </summary>
public sealed class AiWriterService(
    IChatClient client,
    AiChatSettings settings,
    ILogger<AiWriterService> logger)
{
    /// <summary>Prompt tóm tắt diễn biến một bản ghi.</summary>
    public const string SummaryPrompt = """
        Bạn là trợ lý của một công ty lữ hành Việt Nam. Đọc hồ sơ dưới đây và tóm tắt cho nhân viên
        nắm nhanh tình hình.

        # Yêu cầu
        - Bắt đầu bằng 1–2 câu nêu tình hình chung.
        - Sau đó liệt kê tối đa 5 gạch đầu dòng: những gì đã diễn ra, khách quan tâm gì, đang vướng gì.
        - Cuối cùng một dòng "Cần làm tiếp:" với việc cụ thể nhất.
        - CHỈ dùng thông tin trong hồ sơ. Không có trao đổi nào thì nói thẳng là chưa có gì để tóm tắt
          và gợi ý nhân viên ghi lại nội dung liên hệ.
        - Tiếng Việt, ngắn gọn, không lặp lại nguyên văn hồ sơ, không dùng markdown.
        """;

    /// <summary>Prompt soạn tin nhắn gửi khách.</summary>
    public const string DraftPrompt = """
        Bạn là nhân viên kinh doanh của một công ty lữ hành Việt Nam. Đọc hồ sơ dưới đây và soạn MỘT
        tin nhắn để gửi cho khách.

        # Yêu cầu
        - Xưng "em". Hồ sơ có tên khách thì BẮT BUỘC gọi tên trong câu chào, dùng tên gọi (chữ cuối
          cùng) chứ không đọc cả họ tên: "Lý Kim Quân" thì chào "Em chào anh/chị Quân". Chỉ khi hồ sơ
          không có tên mới được chào chung chung.
        - Bám đúng những gì đã trao đổi. TUYỆT ĐỐI không hứa giá, không hứa khuyến mãi, không bịa lịch
          khởi hành hay chỗ trống — những thứ đó không có trong hồ sơ.
        - Chưa có trao đổi nào thì soạn tin làm quen và hỏi nhu cầu, đừng giả vờ đã nói chuyện trước đó.
        - Dài khoảng 3–5 câu, gửi được qua Zalo hoặc tin nhắn. Kết thúc bằng một câu hỏi mở để khách
          dễ trả lời.
        - Chỉ trả về nội dung tin nhắn, không thêm lời dẫn kiểu "Đây là tin nhắn:".
        """;

    /// <summary>
    /// Sinh văn bản. Trả <c>null</c> khi model không trả về gì đọc được — nơi gọi hiện thông báo nhẹ
    /// thay vì dán một chuỗi rỗng lên màn hình.
    /// </summary>
    public async Task<string?> WriteAsync(string systemPrompt, string factSheet, CancellationToken ct)
    {
        var options = new ChatOptions
        {
            ModelId = settings.Model,
            MaxOutputTokens = settings.MaxOutputTokens,
            Temperature = settings.Temperature is null ? null : (float)settings.Temperature.Value,
        };

        var response = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.System, systemPrompt), new ChatMessage(ChatRole.User, factSheet)],
            options,
            ct).ConfigureAwait(false);

        var text = Clean(response.Text);
        if (text is null)
        {
            logger.LogWarning("Model không trả về nội dung nào.");
        }

        return text;
    }

    /// <summary>
    /// Bỏ dấu markdown và lời dẫn thừa. Model vẫn chèn <c>**</c> hay mở đầu bằng "Đây là..." dù prompt
    /// đã cấm; hiện thẳng ra màn hình thì người dùng phải tự xoá trước khi gửi cho khách.
    /// </summary>
    public static string? Clean(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var cleaned = text
            .Replace("**", "", StringComparison.Ordinal)
            .Replace("### ", "", StringComparison.Ordinal)
            .Replace("## ", "", StringComparison.Ordinal)
            .Trim();

        // Lời dẫn kiểu "Đây là tin nhắn gửi khách:" nằm trên một dòng riêng thì cắt bỏ.
        var lines = cleaned.Split('\n');
        if (lines.Length > 1 && lines[0].TrimEnd().EndsWith(':') && lines[0].Length < 80)
        {
            cleaned = string.Join('\n', lines.Skip(1)).Trim();
        }

        return cleaned.Length == 0 ? null : cleaned;
    }
}
