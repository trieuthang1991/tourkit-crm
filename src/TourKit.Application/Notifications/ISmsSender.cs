namespace TourKit.Application.Notifications;

/// <summary>
/// Gửi SMS (kênh marketing SMS). Dev mặc định ghi log; prod dùng provider thật (Twilio/eSMS/…)
/// — thêm 1 implementation đọc cấu hình khi chốt nhà cung cấp, không sửa code gọi. Giống <see cref="IEmailSender"/>.
/// </summary>
public interface ISmsSender
{
    Task SendAsync(string phone, string message, CancellationToken ct = default);
}
