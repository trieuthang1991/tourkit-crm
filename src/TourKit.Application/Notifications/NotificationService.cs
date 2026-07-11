using TourKit.Application.Common;
using TourKit.Shared.Entities;
using TourKit.Shared.Security;

namespace TourKit.Application.Notifications;

/// <summary>
/// Thông báo in-app (legacy Notification). "Của tôi" = user hiện tại (<see cref="ICurrentUserContext"/>).
/// <see cref="PushAsync"/> để hệ đẩy thông báo cho user bất kỳ (vd khi giao việc). Không phụ thuộc dịch vụ ngoài.
/// </summary>
public sealed class NotificationService(
    IRepository<Notification> repo,
    ICurrentUserContext currentUser) : INotificationService
{
    public async Task<IReadOnlyList<NotificationDto>> ListMineAsync(bool unreadOnly)
    {
        var userId = RequireUser();
        var items = await repo.ListAsync(n => n.UserId == userId && (!unreadOnly || !n.IsRead));
        return items.OrderByDescending(n => n.CreatedAt).Select(Map).ToList();
    }

    public async Task<int> UnreadCountAsync()
    {
        var userId = RequireUser();
        var items = await repo.ListAsync(n => n.UserId == userId && !n.IsRead);
        return items.Count;
    }

    public async Task MarkReadAsync(Guid id)
    {
        var userId = RequireUser();
        var entity = await repo.GetByIdAsync(id);
        if (entity is null || entity.UserId != userId)
        {
            throw new NotFoundException();   // không lộ thông báo của người khác
        }

        if (!entity.IsRead)
        {
            entity.IsRead = true;
            repo.Update(entity);
            await repo.SaveChangesAsync();
        }
    }

    public async Task MarkAllReadAsync()
    {
        var userId = RequireUser();
        var unread = await repo.ListAsync(n => n.UserId == userId && !n.IsRead);
        foreach (var n in unread)
        {
            n.IsRead = true;
            repo.Update(n);
        }

        if (unread.Count > 0)
        {
            await repo.SaveChangesAsync();
        }
    }

    public async Task PushAsync(Guid userId, string title, string? message, string? linkUrl = null)
    {
        await repo.AddAsync(new Notification
        {
            UserId = userId,
            Title = title.Trim(),
            Message = message?.Trim(),
            LinkUrl = linkUrl,
            IsRead = false,
        });
        await repo.SaveChangesAsync();
    }

    private Guid RequireUser() =>
        currentUser.UserId ?? throw new ValidationAppException("Không xác định được người dùng hiện tại.");

    private static NotificationDto Map(Notification n) => new(n.Id, n.Title, n.Message, n.LinkUrl, n.IsRead, n.CreatedAt);
}
