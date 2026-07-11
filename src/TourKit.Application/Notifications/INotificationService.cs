namespace TourKit.Application.Notifications;

public interface INotificationService
{
    Task<IReadOnlyList<NotificationDto>> ListMineAsync(bool unreadOnly);
    Task<int> UnreadCountAsync();
    Task MarkReadAsync(Guid id);
    Task MarkAllReadAsync();

    /// <summary>Đẩy thông báo cho 1 user (dùng bởi hệ — vd khi giao việc). Không phụ thuộc user hiện tại.</summary>
    Task PushAsync(Guid userId, string title, string? message, string? linkUrl = null);
}
