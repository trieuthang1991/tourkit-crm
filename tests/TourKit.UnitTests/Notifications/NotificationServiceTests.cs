using TourKit.Application.Common;
using TourKit.Application.Notifications;
using TourKit.Shared.Entities;
using TourKit.Shared.Security;
using TourKit.UnitTests.Catalog; // FakeRepository<T>

namespace TourKit.UnitTests.Notifications;

/// <summary>Test <see cref="NotificationService"/> — push + "của tôi" + đánh dấu đã đọc + cách ly user.</summary>
public class NotificationServiceTests
{
    private sealed class FakeCurrentUser(Guid? userId) : ICurrentUserContext
    {
        public Guid? UserId { get; } = userId;
    }

    private static NotificationService NewService(Guid userId, out FakeRepository<Notification> repo)
    {
        repo = new FakeRepository<Notification>();
        return new NotificationService(repo, new FakeCurrentUser(userId));
    }

    [Fact]
    public async Task Push_then_list_mine_and_unread_count()
    {
        var me = Guid.NewGuid();
        var service = NewService(me, out _);

        await service.PushAsync(me, "Việc mới", "chi tiết", "/work-tasks");

        var mine = await service.ListMineAsync(unreadOnly: false);
        Assert.Single(mine);
        Assert.Equal("Việc mới", mine[0].Title);
        Assert.Equal(1, await service.UnreadCountAsync());
    }

    [Fact]
    public async Task MarkRead_reduces_unread_count()
    {
        var me = Guid.NewGuid();
        var service = NewService(me, out _);
        await service.PushAsync(me, "A", null);
        await service.PushAsync(me, "B", null);

        var mine = await service.ListMineAsync(unreadOnly: true);
        await service.MarkReadAsync(mine[0].Id);

        Assert.Equal(1, await service.UnreadCountAsync());
    }

    [Fact]
    public async Task MarkAllRead_clears_unread()
    {
        var me = Guid.NewGuid();
        var service = NewService(me, out _);
        await service.PushAsync(me, "A", null);
        await service.PushAsync(me, "B", null);

        await service.MarkAllReadAsync();

        Assert.Equal(0, await service.UnreadCountAsync());
    }

    [Fact]
    public async Task ListMine_excludes_other_users_notifications()
    {
        var me = Guid.NewGuid();
        var other = Guid.NewGuid();
        var service = NewService(me, out _);
        await service.PushAsync(other, "Của người khác", null);

        Assert.Empty(await service.ListMineAsync(unreadOnly: false));
        Assert.Equal(0, await service.UnreadCountAsync());
    }

    [Fact]
    public async Task MarkRead_other_users_notification_throws_NotFound()
    {
        var me = Guid.NewGuid();
        var other = Guid.NewGuid();
        var service = NewService(me, out var repo);
        await service.PushAsync(other, "X", null);
        var theirs = (await repo.ListAsync(n => n.UserId == other)).Single();

        await Assert.ThrowsAsync<NotFoundException>(() => service.MarkReadAsync(theirs.Id));
    }
}
