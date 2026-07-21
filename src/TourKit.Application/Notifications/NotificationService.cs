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
    public async Task<IReadOnlyList<NotificationDto>> ListMineAsync(bool unreadOnly, int? take = null)
    {
        var userId = RequireUser();

        // Có giới hạn → PageAsync sắp xếp và cắt NGAY Ở SQL (PageAsync đã OrderByDescending CreatedAt),
        // thay vì kéo toàn bộ thông báo của user về rồi sắp trong bộ nhớ.
        if (take is int n && n > 0)
        {
            var (page, _) = await repo.PageAsync(1, n, x => x.UserId == userId && (!unreadOnly || !x.IsRead));
            return page.Select(Map).ToList();
        }

        var items = await repo.ListAsync(x => x.UserId == userId && (!unreadOnly || !x.IsRead));
        return items.OrderByDescending(x => x.CreatedAt).Select(Map).ToList();
    }

    /// <summary>Đếm bằng COUNT ở SQL — hàm này chạy MỖI LẦN render trang (chuông thông báo).</summary>
    public Task<int> UnreadCountAsync()
    {
        var userId = RequireUser();
        return repo.CountAsync(n => n.UserId == userId && !n.IsRead);
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

    public async Task PushAsync(Guid userId, string title, string? message, string? linkUrl = null, string type = "system")
    {
        await repo.AddAsync(new Notification
        {
            UserId = userId,
            Title = title.Trim(),
            Message = message?.Trim(),
            LinkUrl = linkUrl,
            Type = string.IsNullOrWhiteSpace(type) ? "system" : type.Trim(),
            IsRead = false,
        });
        await repo.SaveChangesAsync();
    }

    private Guid RequireUser() =>
        currentUser.UserId ?? throw new ValidationAppException("Không xác định được người dùng hiện tại.");

    private static NotificationDto Map(Notification n) => new(n.Id, n.Title, n.Message, n.LinkUrl, n.Type, n.IsRead, n.CreatedAt);
}
