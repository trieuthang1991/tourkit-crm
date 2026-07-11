using Microsoft.Extensions.Logging;
using TourKit.Application.Notifications;

namespace TourKit.Infrastructure.Notifications;

/// <summary>Mặc định dev: ghi log thay vì gửi SMS thật (không cần provider). Prod thay bằng implementation
/// provider thật (Twilio/eSMS…) đọc cấu hình — đăng ký theo Sms:Provider trong Program.cs.</summary>
public sealed partial class LogSmsSender(ILogger<LogSmsSender> logger) : ISmsSender
{
    public Task SendAsync(string phone, string message, CancellationToken ct = default)
    {
        LogSms(logger, phone, message.Length);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "[Sms:Log] To={Phone} Length={Length}")]
    private static partial void LogSms(ILogger logger, string phone, int length);
}
