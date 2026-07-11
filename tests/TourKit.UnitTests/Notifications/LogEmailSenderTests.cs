using Microsoft.Extensions.Logging.Abstractions;
using TourKit.Infrastructure.Notifications;

namespace TourKit.UnitTests.Notifications;

public class LogEmailSenderTests
{
    [Fact]
    public async Task SendAsync_dev_log_provider_does_not_throw()
    {
        var sender = new LogEmailSender(NullLogger<LogEmailSender>.Instance);

        await sender.SendAsync("agent@example.com", "Báo giá mới", "Nội dung");
    }
}
