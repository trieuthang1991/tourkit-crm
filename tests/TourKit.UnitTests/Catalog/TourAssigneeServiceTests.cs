using TourKit.Application.Catalog;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Common;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.UnitTests.Catalog;

/// <summary>Test <see cref="TourAssigneeService"/> (replace-all người phụ trách) qua fake repo in-memory.</summary>
public class TourAssigneeServiceTests
{
    private static TourAssigneeService NewService(
        out FakeRepository<TourAssignee> repo, out FakeRepository<Tour> tourRepo)
    {
        repo = new FakeRepository<TourAssignee>();
        tourRepo = new FakeRepository<Tour>();
        return new TourAssigneeService(repo, tourRepo);
    }

    [Fact]
    public async Task ReplaceAsync_unknown_tour_throws_NotFoundException()
    {
        var service = NewService(out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.ReplaceAsync(
            Guid.NewGuid(), [new AssigneeDto(Guid.Empty, Guid.NewGuid(), AssigneeRole.Manager)]));
    }

    [Fact]
    public async Task ReplaceAsync_replaces_all_assignees_for_tour()
    {
        var service = NewService(out _, out var tourRepo);
        var template = new TourTemplate { Code = "T-1", Title = "Đà Nẵng" };
        await tourRepo.AddAsync(template);
        await tourRepo.SaveChangesAsync();

        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        await service.ReplaceAsync(template.Id,
        [
            new AssigneeDto(Guid.Empty, userA, AssigneeRole.Manager),
            new AssigneeDto(Guid.Empty, userB, AssigneeRole.Watcher),
        ]);

        var list = await service.ListAsync(template.Id);
        Assert.Equal(2, list.Count);

        // Lần thay thứ 2 phải XOÁ toàn bộ dòng cũ, không cộng dồn.
        await service.ReplaceAsync(template.Id, [new AssigneeDto(Guid.Empty, userA, AssigneeRole.Manager)]);
        var replaced = await service.ListAsync(template.Id);
        Assert.Single(replaced);
    }
}
