using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using TourKit.Application.Notifications;

namespace TourKit.Infrastructure.Notifications;

/// <summary>Gửi email qua SMTP (prod). Đọc Host/Port/User/Password/From từ cấu hình Email — user điền để kích hoạt.</summary>
public sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Credentials = new NetworkCredential(_options.User, _options.Password),
        };
        using var message = new MailMessage(_options.From, to, subject, body);
        await client.SendMailAsync(message, ct);
    }
}
