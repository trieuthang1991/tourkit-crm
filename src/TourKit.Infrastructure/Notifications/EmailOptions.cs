namespace TourKit.Infrastructure.Notifications;

/// <summary>Cấu hình email. Provider "Log" (dev, không cần credential) hoặc "Smtp" (prod, điền Host/User/Password).</summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Provider { get; set; } = "Log";   // Log | Smtp
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    /// <summary>Nhận cả dạng "ENC:" (Crypton, dùng chung hệ sinh thái TourKit) — tự giải khi gửi.</summary>
    public string User { get; set; } = string.Empty;

    /// <summary>Nhận cả dạng "ENC:" (Crypton) — tự giải khi gửi.</summary>
    public string Password { get; set; } = string.Empty;

    public string From { get; set; } = "no-reply@tourkit.vn";
    public string FromName { get; set; } = "TourKit";

    /// <summary>Có gửi thật qua SMTP không. So sánh nằm ở đây để mọi nơi hỏi cùng một câu.</summary>
    public bool IsSmtp => string.Equals(Provider, "Smtp", StringComparison.OrdinalIgnoreCase);
}
