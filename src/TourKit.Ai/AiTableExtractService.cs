using System.Text;
using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace TourKit.Ai;

/// <summary>
/// Đọc một tài liệu báo giá dạng văn xuôi (PDF/DOC/DOCX) rồi DỰNG LẠI thành bảng theo ĐÚNG các cột
/// hệ thống yêu cầu.
///
/// Vì sao chỉ dừng ở đây: model có đúng MỘT việc là khớp thông tin trong tài liệu vào tên cột cho
/// sẵn. Toàn bộ khâu kiểm dữ liệu — số có hợp lệ không, ngày có parse được không, giai đoạn có ngược
/// không — vẫn do lớp kiểm tất định làm, y hệt đường Excel/CSV. Nhờ vậy model đoán sai cũng chỉ ra
/// một dòng hỏng có lý do rõ ràng ở bảng xem trước, chứ không đẩy được số rác vào bảng giá.
///
/// Cũng vì thế đầu ra CỐ Ý nghèo nàn: chỉ là chuỗi thô theo từng cột, không phải kiểu dữ liệu đã
/// diễn giải. Để model tự trả về ngày tháng hay số đã chuẩn hoá là giao cho nó phần việc mà mã tất
/// định làm đúng hơn và kiểm thử được.
/// </summary>
public sealed class AiTableExtractService(
    IChatClient client,
    AiChatSettings settings,
    ILogger<AiTableExtractService> logger)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private const string Frame = """
        Bạn đọc bảng giá dịch vụ do nhà cung cấp du lịch gửi và chuyển nó thành bảng có cấu trúc.

        # Nguyên tắc
        - CHỈ lấy thông tin có thật trong tài liệu. Không suy đoán, không tự điền giá hay ngày.
        - Ô nào tài liệu không nói thì để chuỗi rỗng "". KHÔNG được đoán cho đủ.
        - Mỗi gói giá / mỗi hạng phòng / mỗi chặng bay là MỘT dòng riêng.
        - Giữ NGUYÊN VĂN giá trị đọc được: số cứ chép y như trong tài liệu (kể cả dấu chấm phân cách),
          ngày cứ chép y như trong tài liệu. KHÔNG tự đổi định dạng, không tự làm tròn.
        - Tài liệu không phải bảng giá thì trả về mảng rỗng.

        # Định dạng trả về
        Chỉ trả về JSON, không lời dẫn, không bọc trong khối mã. Mỗi phần tử của "rows" là một đối
        tượng mà KEY phải trùng ĐÚNG tên cột được liệt kê bên dưới:
        {"rows":[{"<tên cột>":"<giá trị>"}]}
        """;

    /// <summary>
    /// Bóc tài liệu thành các dòng theo <paramref name="cot"/>. Trả về danh sách rỗng khi model trả
    /// về thứ không đọc được — bên gọi hiển thị "không đọc được gì" chứ không dựng bảng nửa vời.
    /// </summary>
    public async Task<IReadOnlyList<IReadOnlyDictionary<string, string>>> ExtractAsync(
        string vanBan, IReadOnlyList<string> cot, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cot);

        var options = new ChatOptions
        {
            ModelId = settings.Model,
            MaxOutputTokens = settings.MaxOutputTokens,
            Temperature = settings.Temperature is null ? null : (float)settings.Temperature.Value,
            ResponseFormat = ChatResponseFormat.Json,
        };

        var response = await client.GetResponseAsync(
            [new ChatMessage(ChatRole.System, Prompt(cot)), new ChatMessage(ChatRole.User, vanBan)],
            options,
            ct).ConfigureAwait(false);

        var rows = Doc(response.Text);
        if (rows is null)
        {
            logger.LogWarning("Không đọc được bảng do AI trả về. Model trả về: {Text}", response.Text);
            return [];
        }

        return rows;
    }

    private static string Prompt(IReadOnlyList<string> cot)
    {
        var sb = new StringBuilder(Frame).Append("\n\n# Các cột phải điền\n");
        foreach (var c in cot)
        {
            sb.Append("- \"").Append(c).Append("\"\n");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Đọc JSON model trả về. Cắt lấy phần trong cặp ngoặc ngoài cùng trước khi parse: kể cả khi đã
    /// yêu cầu JSON thuần, model vẫn hay kèm lời dẫn hoặc bọc trong ```json.
    /// </summary>
    private static List<IReadOnlyDictionary<string, string>>? Doc(string? text)
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
            var payload = JsonSerializer.Deserialize<Payload>(text[start..(end + 1)], Json);
            if (payload?.Rows is null)
            {
                return null;
            }

            // Giá trị về dạng JsonElement vì model có thể trả số thay vì chuỗi cho ô giá — ép hết về
            // chuỗi thô, đúng tinh thần "mã tất định mới là chỗ diễn giải kiểu dữ liệu".
            return payload.Rows
                .Select(r => (IReadOnlyDictionary<string, string>)r.ToDictionary(
                    kv => kv.Key,
                    kv => kv.Value.ValueKind == JsonValueKind.String ? kv.Value.GetString() ?? "" : kv.Value.ToString(),
                    StringComparer.Ordinal))
                .ToList();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed record Payload(List<Dictionary<string, JsonElement>>? Rows);
}
