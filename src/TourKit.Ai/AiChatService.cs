using System.Text.Json;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using TourKit.Ai.Abstractions;

namespace TourKit.Ai;

/// <summary>Một khối dữ liệu có cấu trúc do công cụ sinh ra, để giao diện vẽ bảng.</summary>
/// <param name="Tool">Tên công cụ đã chạy.</param>
/// <param name="Data">Dữ liệu bảng (JsonElement).</param>
/// <param name="LinkUrl">Màn hình tương ứng để xem đầy đủ.</param>
public sealed record AiAnswerBlock(string Tool, object? Data, string? LinkUrl);

/// <summary>Câu trả lời hoàn chỉnh gửi về giao diện.</summary>
/// <param name="Text">Lời của trợ lý.</param>
/// <param name="Blocks">Các bảng số liệu kèm theo.</param>
public sealed record AiAnswer(string Text, IReadOnlyList<AiAnswerBlock> Blocks);

/// <summary>Thông số một lượt hỏi, lấy từ <c>Ai:Features:Assistant</c>.</summary>
/// <param name="Model">Mã model của hãng.</param>
/// <param name="MaxOutputTokens">Giới hạn độ dài câu trả lời.</param>
/// <param name="MaxToolRounds">Số vòng gọi công cụ tối đa.</param>
/// <param name="Temperature">Bỏ trống với model suy luận — chúng trả 400 khi nhận tham số này.</param>
public sealed record AiChatSettings(string Model, int MaxOutputTokens, int MaxToolRounds, double? Temperature = null);

/// <summary>
/// Vòng lặp trợ lý: gọi model → chạy công cụ model yêu cầu → gọi model lại với kết quả, tối đa
/// <c>MaxToolRounds</c> vòng.
///
/// Cố ý KHÔNG dùng <c>UseFunctionInvocation()</c> của Microsoft.Extensions.AI dù nó tự chạy được vòng
/// lặp này: tự chạy thì ta mất hai thứ bắt buộc phải có — chặn cứng số vòng, và giữ lại phần dữ liệu
/// có cấu trúc mà giao diện cần để vẽ bảng. Phần "nói chuyện với hãng nào" vẫn để thư viện lo.
/// </summary>
public sealed class AiChatService(
    IChatClient client,
    AiToolRegistry registry,
    AiChatSettings settings,
    ILogger<AiChatService> logger)
{
    /// <summary>Hỏi trợ lý một câu, với bộ quyền của người đang hỏi.</summary>
    public async Task<AiAnswer> AskAsync(string question, IReadOnlySet<string> perms, CancellationToken ct)
    {
        var allowed = registry.For(perms);

        // Không công cụ nào thì đừng gọi model. Thử nghiệm với tài khoản không có quyền báo cáo cho
        // thấy model nhận danh sách công cụ RỖNG vẫn trả lời "chờ tôi lấy dữ liệu cho bạn" rồi im —
        // hứa một thứ nó không bao giờ làm được. Trả lời thẳng vừa đúng vừa khỏi tốn một lượt gọi.
        if (allowed.Count == 0)
        {
            logger.LogInformation("Người dùng không có quyền với công cụ nào — không gọi model.");
            return new AiAnswer(
                "Tài khoản của bạn chưa được cấp quyền xem các mục số liệu mà tôi tra được, nên tôi chưa giúp được gì. " +
                "Bạn liên hệ quản trị viên để được cấp quyền nhé.",
                []);
        }

        var blocks = new List<AiAnswerBlock>();

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, AiPrompts.System),
            new(ChatRole.User, question),
        };

        var options = new ChatOptions
        {
            ModelId = settings.Model,
            MaxOutputTokens = settings.MaxOutputTokens,
            Temperature = settings.Temperature is null ? null : (float)settings.Temperature.Value,
            Tools = [.. allowed.Select(t => (AITool)t.Function)],
        };

        for (var round = 1; round <= settings.MaxToolRounds; round++)
        {
            var response = await client.GetResponseAsync(messages, options, ct).ConfigureAwait(false);
            messages.AddRange(response.Messages);

            var calls = response.Messages
                .SelectMany(m => m.Contents)
                .OfType<FunctionCallContent>()
                .ToList();

            if (calls.Count == 0)
            {
                return new AiAnswer(Clean(response.Text), blocks);
            }

            var results = new List<AIContent>(calls.Count);
            foreach (var call in calls)
            {
                results.Add(await RunOneAsync(call, allowed, blocks, ct).ConfigureAwait(false));
            }

            messages.Add(new ChatMessage(ChatRole.Tool, results));
        }

        logger.LogWarning("Trợ lý chạm trần {Rounds} vòng gọi công cụ mà chưa chốt câu trả lời.", settings.MaxToolRounds);
        return new AiAnswer(
            "Câu hỏi này cần tra nhiều mục quá nên tôi chưa hoàn tất được. Bạn thử hỏi gọn lại từng ý một nhé.",
            blocks);
    }

    /// <summary>
    /// Chạy một lời gọi công cụ. Mọi hỏng hóc đều trả lỗi NGƯỢC VỀ CHO MODEL thay vì ném ra ngoài —
    /// model đọc được thông điệp lỗi thì nó tự sửa hoặc tự nói với người dùng, còn ném ra ngoài thì cả
    /// lượt hỏi mất trắng.
    /// </summary>
    private async Task<FunctionResultContent> RunOneAsync(
        FunctionCallContent call, IReadOnlyList<IAiTool> allowed, List<AiAnswerBlock> blocks, CancellationToken ct)
    {
        var name = call.Name;
        var tool = AiToolRegistry.Find(allowed, name);
        if (tool is null)
        {
            // Có thể model bịa tên, cũng có thể nó nhớ một công cụ mà người này không có quyền thấy.
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Model gọi công cụ không có trong danh sách đã lọc: {Name}", name);
            }

            return new FunctionResultContent(call.CallId, $"Công cụ \"{name}\" không tồn tại hoặc bạn không được dùng.");
        }

        try
        {
            var args = new AIFunctionArguments(call.Arguments);
            var raw = await tool.Function.InvokeAsync(args, ct).ConfigureAwait(false);
            var result = Unwrap(raw);

            if (result.Data is not null || result.LinkUrl is not null)
            {
                blocks.Add(new AiAnswerBlock(call.Name, result.Data, result.LinkUrl));
            }

            return new FunctionResultContent(call.CallId, result.Text);
        }
#pragma warning disable CA1031 // Lỗi của MỘT công cụ không được làm hỏng cả lượt hỏi — đưa về cho model diễn đạt lại.
        catch (Exception ex) when (ex is not OperationCanceledException)
#pragma warning restore CA1031
        {
            logger.LogError(ex, "Công cụ {Name} lỗi khi trợ lý gọi.", call.Name);
            return new FunctionResultContent(call.CallId, $"Lỗi khi tra số liệu: {ex.Message}");
        }
    }

    /// <summary>
    /// <c>AIFunction.InvokeAsync</c> trả về kết quả ĐÃ SERIALIZE thành <see cref="JsonElement"/>, không
    /// phải đối tượng gốc. Đọc lại bằng đúng bộ tuỳ chọn mà thư viện dùng để ghi ra, chứ không mò theo
    /// tên thuộc tính.
    /// </summary>
    private static AiToolResult Unwrap(object? raw) => raw switch
    {
        AiToolResult direct => direct,
        JsonElement json => JsonSerializer.Deserialize<AiToolResult>(json, AIJsonUtilities.DefaultOptions)
                            ?? new AiToolResult("Công cụ không trả về gì."),
        null => new AiToolResult("Công cụ không trả về gì."),
        _ => new AiToolResult(raw.ToString() ?? ""),
    };

    /// <summary>Model đôi khi vẫn chèn tiêu đề markdown dù prompt đã cấm — bỏ đi cho gọn.</summary>
    private static string Clean(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? "Tôi chưa có câu trả lời cho ý này."
            : text.Replace("**", "", StringComparison.Ordinal).Replace("### ", "", StringComparison.Ordinal).Trim();
}
