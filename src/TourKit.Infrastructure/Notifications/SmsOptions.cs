namespace TourKit.Infrastructure.Notifications;

/// <summary>Cấu hình SMS. Provider "Log" (dev, mặc định). Prod: thêm provider thật + đổi giá trị này.</summary>
public sealed class SmsOptions
{
    public const string SectionName = "Sms";

    public string Provider { get; set; } = "Log";   // Log | (Twilio/eSMS… khi tích hợp)
}
