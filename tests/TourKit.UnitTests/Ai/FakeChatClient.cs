using Microsoft.Extensions.AI;

namespace TourKit.UnitTests.Ai;

/// <summary>
/// Client giả chạy theo kịch bản dựng sẵn: mỗi lần được gọi trả về phần tử kế tiếp. Ghi lại danh sách
/// công cụ nó NHÌN THẤY ở từng lượt để test kiểm tra bộ lọc quyền đã áp đúng trước khi gửi đi.
/// </summary>
internal sealed class FakeChatClient(params ChatMessage[] script) : IChatClient
{
    public int Calls { get; private set; }

    public List<IReadOnlyList<string>> ToolNamesSeen { get; } = [];

    public List<IReadOnlyList<ChatMessage>> HistoriesSeen { get; } = [];

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        ToolNamesSeen.Add([.. (options?.Tools ?? []).Select(t => t.Name)]);
        HistoriesSeen.Add([.. messages]);

        var i = Calls++;
        // Hết kịch bản thì trả lời chốt — mô phỏng model ngừng gọi công cụ.
        var message = i < script.Length ? script[i] : new ChatMessage(ChatRole.Assistant, "xong");
        return Task.FromResult(new ChatResponse(message));
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
        // Không giữ tài nguyên nào.
    }

    /// <summary>Dựng một lượt trả lời trong đó model yêu cầu gọi công cụ.</summary>
    public static ChatMessage CallsTool(string callId, string name) =>
        new(ChatRole.Assistant, (IList<AIContent>)[new FunctionCallContent(callId, name, new Dictionary<string, object?>())]);

    /// <summary>Dựng một lượt trả lời bằng lời.</summary>
    public static ChatMessage Says(string text) => new(ChatRole.Assistant, text);
}
