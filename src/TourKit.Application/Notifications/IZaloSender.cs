namespace TourKit.Application.Notifications;

/// <summary>
/// Gửi tin nhắn Zalo OA/ZNS (kênh marketing Zalo). Dev mặc định ghi log; prod dùng Zalo OA API thật
/// — thêm 1 implementation đọc cấu hình khi có tài khoản OA, không sửa code gọi. Giống <see cref="ISmsSender"/>.
/// </summary>
public interface IZaloSender
{
    Task SendAsync(string zaloId, string message, CancellationToken ct = default);
}
