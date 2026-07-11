namespace TourKit.Application.Notifications;

/// <summary>
/// Gửi email (conventions §8, B2B §4.2.9). Dev mặc định ghi log; prod dùng SMTP (đổi bằng cấu hình
/// Email:Provider, không sửa code gọi) — giống mô hình IFileStorage.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
}
