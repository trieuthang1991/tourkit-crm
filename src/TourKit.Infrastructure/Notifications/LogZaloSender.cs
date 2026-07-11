using Microsoft.Extensions.Logging;
using TourKit.Application.Notifications;

namespace TourKit.Infrastructure.Notifications;

/// <summary>Mặc định dev: ghi log thay vì gửi Zalo thật (không cần OA). Prod thay bằng implementation
/// Zalo OA API thật — đăng ký theo Zalo:Provider trong Program.cs.</summary>
public sealed partial class LogZaloSender(ILogger<LogZaloSender> logger) : IZaloSender
{
    public Task SendAsync(string zaloId, string message, CancellationToken ct = default)
    {
        LogZalo(logger, zaloId, message.Length);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "[Zalo:Log] To={ZaloId} Length={Length}")]
    private static partial void LogZalo(ILogger logger, string zaloId, int length);
}
