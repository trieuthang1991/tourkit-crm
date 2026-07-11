namespace TourKit.Infrastructure.Notifications;

/// <summary>Cấu hình Zalo. Provider "Log" (dev, mặc định). Prod: thêm provider Zalo OA thật + đổi giá trị này.</summary>
public sealed class ZaloOptions
{
    public const string SectionName = "Zalo";

    public string Provider { get; set; } = "Log";   // Log | (Zalo OA khi tích hợp)
}
