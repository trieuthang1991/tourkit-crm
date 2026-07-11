using TourKit.Application.Common;
using TourKit.Application.Work;
using TourKit.Application.Work.Validators;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;
using TourKit.UnitTests.Catalog; // FakeRepository<T>

namespace TourKit.UnitTests.Work;

/// <summary>Test <see cref="WorkTaskService"/> — CRUD + lọc + validate người được giao + resolve tên.</summary>
public class WorkTaskServiceTests
{
    private static WorkTaskService NewService(out FakeRepository<WorkTask> repo, out FakeRepository<User> userRepo)
    {
        repo = new FakeRepository<WorkTask>();
        userRepo = new FakeRepository<User>();
        return new WorkTaskService(repo, userRepo, new CreateWorkTaskValidator(), new UpdateWorkTaskValidator());
    }

    private static CreateWorkTaskDto NewDto(string title = "Gọi khách xác nhận", Guid? assignee = null) =>
        new(title, "mô tả", assignee, null, (int)WorkTaskPriority.Normal, (int)WorkTaskStatus.Todo, null);

    [Fact]
    public async Task CreateAsync_persists_and_returns_dto()
    {
        var service = NewService(out var repo, out _);

        var dto = await service.CreateAsync(NewDto());

        Assert.Equal("Gọi khách xác nhận", dto.Title);
        Assert.Equal((int)WorkTaskStatus.Todo, dto.Status);
        Assert.NotNull(await repo.GetByIdAsync(dto.Id));
    }

    [Fact]
    public async Task CreateAsync_resolves_assignee_name()
    {
        var service = NewService(out _, out var userRepo);
        var user = new User { Email = "a@b.c", FullName = "Nguyễn Văn A" };
        await userRepo.AddAsync(user);
        await userRepo.SaveChangesAsync();

        var dto = await service.CreateAsync(NewDto(assignee: user.Id));

        Assert.Equal(user.Id, dto.AssigneeUserId);
        Assert.Equal("Nguyễn Văn A", dto.AssigneeName);
    }

    [Fact]
    public async Task CreateAsync_unknown_assignee_throws()
    {
        var service = NewService(out _, out _);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(NewDto(assignee: Guid.NewGuid())));
    }

    [Fact]
    public async Task CreateAsync_empty_title_throws()
    {
        var service = NewService(out _, out _);

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(NewDto(title: "")));
    }

    [Fact]
    public async Task CreateAsync_invalid_status_throws()
    {
        var service = NewService(out _, out _);
        var bad = NewDto() with { Status = 9 };

        await Assert.ThrowsAsync<ValidationAppException>(() => service.CreateAsync(bad));
    }

    [Fact]
    public async Task ListAsync_filters_by_assignee_and_status()
    {
        var service = NewService(out _, out var userRepo);
        var u1 = new User { Email = "a@b.c", FullName = "A" };
        var u2 = new User { Email = "b@b.c", FullName = "B" };
        await userRepo.AddAsync(u1);
        await userRepo.AddAsync(u2);
        await userRepo.SaveChangesAsync();

        await service.CreateAsync(NewDto("T1", u1.Id));
        var t2 = await service.CreateAsync(NewDto("T2", u2.Id));
        await service.UpdateAsync(t2.Id, new UpdateWorkTaskDto("T2", null, u2.Id, null, 1, (int)WorkTaskStatus.Done, null));

        var forU1 = await service.ListAsync(u1.Id, null);
        Assert.Single(forU1);
        Assert.Equal("T1", forU1[0].Title);

        var done = await service.ListAsync(null, (int)WorkTaskStatus.Done);
        Assert.Single(done);
        Assert.Equal("T2", done[0].Title);
    }

    [Fact]
    public async Task DeleteAsync_unknown_throws_NotFound()
    {
        var service = NewService(out _, out _);

        await Assert.ThrowsAsync<NotFoundException>(() => service.DeleteAsync(Guid.NewGuid()));
    }
}
