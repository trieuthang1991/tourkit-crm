using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace TourKit.Ai;

/// <summary>
/// Client giả: không gọi ra ngoài, chỉ ghi log và trả một câu cố định. Dùng khi chưa có khoá — cùng
/// khuôn với <c>LogEmailSender</c>/<c>LogSmsSender</c> đã có sẵn trong hệ thống.
///
/// Có mặt nó là lý do một máy chưa cấu hình khoá vẫn chạy được toàn bộ màn hình trợ lý: đổi
/// <c>Ai:Features:Assistant:Provider</c> sang <c>log</c> là xong, không phải sửa code.
/// </summary>
public sealed class LogChatClient(ILogger<LogChatClient> logger) : IChatClient
{
    private const string Reply =
        "Trợ lý đang chạy ở chế độ thử (chưa cấu hình khoá AI thật) nên tôi chưa tra được số liệu. " +
        "Bạn đặt khoá cho nhà cung cấp trong cấu hình rồi hỏi lại nhé.";

    /// <inheritdoc/>
    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messages);
        if (logger.IsEnabled(LogLevel.Information))
        {
            var count = messages.Count();
            var tools = options?.Tools?.Count ?? 0;
            logger.LogInformation("[AI giả] {Count} tin nhắn, {Tools} công cụ khả dụng.", count, tools);
        }

        return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, Reply)));
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetResponseAsync(messages, options, cancellationToken).ConfigureAwait(false);
        foreach (var update in response.ToChatResponseUpdates())
        {
            yield return update;
        }
    }

    /// <inheritdoc/>
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        return serviceKey is null && serviceType.IsInstanceOfType(this) ? this : null;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Không giữ tài nguyên nào.
    }
}
