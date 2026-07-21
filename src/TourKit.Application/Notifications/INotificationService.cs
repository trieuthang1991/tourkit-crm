namespace TourKit.Application.Notifications;

public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> ListMineAsync(bool unreadOnly, int? take = null);
    Task<int> UnreadCountAsync();
    Task MarkReadAsync(Guid id);
    Task MarkAllReadAsync();

    /// <summary>Đẩy thông báo cho 1 user (dùng bởi hệ — vd khi giao việc). Không phụ thuộc user hiện tại.
    /// <paramref name="type"/>: phân loại (approval/task/marketing/system) để FE hiển thị icon/lọc.</summary>
    Task PushAsync(Guid userId, string title, string? message, string? linkUrl = null, string type = "system");
}
