using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using TourKit.Ai.Abstractions;

namespace TourKit.Ai;

/// <summary>Adapter cho <c>Kind = "Log"</c> — dựng <see cref="LogChatClient"/>, không cần khoá.</summary>
public sealed class LogChatClientProvider(ILoggerFactory loggers) : IChatClientProvider
{
    /// <inheritdoc/>
    public string Kind => "Log";

    /// <inheritdoc/>
    public IChatClient Create(AiProviderOptions provider, string model) =>
        new LogChatClient(loggers.CreateLogger<LogChatClient>());
}
