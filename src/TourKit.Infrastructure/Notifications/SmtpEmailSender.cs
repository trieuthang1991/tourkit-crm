using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using TourKit.Application.Notifications;
using TourKit.Shared.Security;

namespace TourKit.Infrastructure.Notifications;

/// <summary>
/// Gửi email qua SMTP (prod). Đọc Host/Port/User/Password/From từ cấu hình Email.
/// User/Password nhận cả dạng "ENC:" (Crypton — dán thẳng chuỗi dùng chung hệ TourKit).
/// Body gửi dạng HTML (link đặt lại mật khẩu, email marketing...).
/// </summary>
public sealed class SmtpEmailSender(IOptions<EmailOptions> options) : IEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public async Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            Credentials = new NetworkCredential(Crypton.Unwrap(_options.User), Crypton.Unwrap(_options.Password)),
        };
        using var message = new MailMessage
        {
            From = new MailAddress(_options.From, _options.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true,
        };
        message.To.Add(to);
        await client.SendMailAsync(message, ct);
    }
}
