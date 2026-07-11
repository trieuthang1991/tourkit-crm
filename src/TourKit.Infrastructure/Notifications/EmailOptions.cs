namespace TourKit.Infrastructure.Notifications;

/// <summary>Cấu hình email. Provider "Log" (dev, không cần credential) hoặc "Smtp" (prod, điền Host/User/Password).</summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string Provider { get; set; } = "Log";   // Log | Smtp
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = "no-reply@tourkit.vn";
}
