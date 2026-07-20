using Microsoft.Extensions.Logging;
using TourKit.Application.Notifications;

namespace TourKit.Infrastructure.Notifications;

/// <summary>Mặc định dev: ghi log thay vì gửi thật (không cần credential). Prod dùng <see cref="SmtpEmailSender"/>.</summary>
public sealed partial class LogEmailSender(ILogger<LogEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        // Ghi cả body: dev cần đọc được link (đặt lại mật khẩu, xác nhận...) mà không có SMTP.
        LogEmail(logger, to, subject, body);
        return Task.CompletedTask;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "[Email:Log] To={To} Subject={Subject}\n{Body}")]
    private static partial void LogEmail(ILogger logger, string to, string subject, string body);
}
