using System.ClientModel;
using Microsoft.Extensions.AI;
using OpenAI;
using TourKit.Ai.Abstractions;

namespace TourKit.Ai.OpenAiCompatible;

/// <summary>
/// Adapter cho MỌI hãng nói giao thức OpenAI: DeepSeek, OpenAI, Groq, Together, Ollama…
///
/// Một adapter phục vụ nhiều hãng vì khác biệt giữa chúng nằm ở <c>BaseUrl</c> + tên model, không nằm
/// ở giao thức. Hãng nào có giao thức riêng (Anthropic, Gemini) thì viết project adapter riêng — đó
/// chính là lý do tách adapter thành project độc lập ngay từ đầu.
///
/// Project này CHỈ tham chiếu <c>TourKit.Ai.Abstractions</c>: nó không được thấy vòng lặp chat, nên
/// sửa vòng lặp không bao giờ bắt sửa lại adapter.
/// </summary>
public sealed class OpenAiCompatibleChatClientProvider : IChatClientProvider
{
    /// <inheritdoc/>
    public string Kind => "OpenAiCompatible";

    /// <inheritdoc/>
    public IChatClient Create(AiProviderOptions provider, string model)
    {
        ArgumentNullException.ThrowIfNull(provider);

        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(provider.BaseUrl),
            NetworkTimeout = TimeSpan.FromSeconds(provider.TimeoutSeconds),
        };

        var client = new OpenAIClient(new ApiKeyCredential(provider.ApiKey), options);
        return client.GetChatClient(model).AsIChatClient();
    }
}
