using TourKit.Application.Common;
using TourKit.Application.Work;
using TourKit.Shared.Entities;
using TourKit.UnitTests.Catalog; // FakeRepository<T>

namespace TourKit.UnitTests.Work;

/// <summary>
/// Test <see cref="WorkflowService"/> — board Kanban động (legacy Workflow/SectionWork):
/// gieo cột mặc định, thêm/sửa/xoá/sắp cột, kéo thẻ việc giữa cột, tách việc khi xoá board/cột.
/// </summary>
public class WorkflowServiceTests
{
    private static WorkflowService NewService(
        out FakeRepository<Workflow> repo, out FakeRepository<WorkflowSection> sectionRepo,
        out FakeRepository<WorkTask> taskRepo)
    {
        repo = new FakeRepository<Workflow>();
        sectionRepo = new FakeRepository<WorkflowSection>();
        taskRepo = new FakeRepository<WorkTask>();
        return new WorkflowService(repo, sectionRepo, taskRepo, new FakeRepository<User>());
    }

    [Fact]
    public async Task CreateAsync_seeds_three_default_sections()
    {
        var service = NewService(out _, out var sectionRepo, out _);

        var board = await service.CreateAsync(new CreateWorkflowDto("Điều hành tour hè", null, null));

        Assert.Equal(3, board.SectionCount);
        var sections = await sectionRepo.ListAsync(s => s.WorkflowId == board.Id);
        Assert.Equal(new[] { "Cần làm", "Đang làm", "Hoàn thành" }, sections.OrderBy(s => s.Sort).Select(s => s.Name));
    }

    [Fact]
    public async Task GetBoardAsync_groups_tasks_into_their_section()
    {
        var service = NewService(out _, out var sectionRepo, out var taskRepo);
        var board = await service.CreateAsync(new CreateWorkflowDto("Board", null, null));
        var sections = (await sectionRepo.ListAsync(s => s.WorkflowId == board.Id)).OrderBy(s => s.Sort).ToList();

        var task = new WorkTask { Title = "Đặt xe", WorkflowId = board.Id, SectionId = sections[1].Id };
        await taskRepo.AddAsync(task);
        await taskRepo.SaveChangesAsync();

        var detail = await service.GetBoardAsync(board.Id);
        Assert.Equal(3, detail.Columns.Count);
        Assert.Empty(detail.Columns[0].Tasks);
        Assert.Single(detail.Columns[1].Tasks);
        Assert.Equal("Đặt xe", detail.Columns[1].Tasks[0].Title);
    }

    [Fact]
    public async Task AddSection_appends_with_next_sort()
    {
        var service = NewService(out _, out _, out _);
        var board = await service.CreateAsync(new CreateWorkflowDto("Board", null, null));

        var added = await service.AddSectionAsync(board.Id, new CreateSectionDto("Chờ duyệt", "#f00", null));
        Assert.Equal(3, added.Sort);   // sau 3 cột mặc định (0,1,2)
    }

    [Fact]
    public async Task MoveTaskAsync_moves_card_to_target_section()
    {
        var service = NewService(out _, out var sectionRepo, out var taskRepo);
        var board = await service.CreateAsync(new CreateWorkflowDto("Board", null, null));
        var sections = (await sectionRepo.ListAsync(s => s.WorkflowId == board.Id)).OrderBy(s => s.Sort).ToList();
        var task = new WorkTask { Title = "Việc", WorkflowId = board.Id, SectionId = sections[0].Id };
        await taskRepo.AddAsync(task);
        await taskRepo.SaveChangesAsync();

        await service.MoveTaskAsync(board.Id, task.Id, new MoveTaskDto(sections[2].Id));

        Assert.Equal(sections[2].Id, (await taskRepo.GetByIdAsync(task.Id))!.SectionId);
    }

    [Fact]
    public async Task MoveTaskAsync_to_foreign_section_throws()
    {
        var service = NewService(out _, out _, out var taskRepo);
        var board = await service.CreateAsync(new CreateWorkflowDto("Board", null, null));
        var task = new WorkTask { Title = "Việc", WorkflowId = board.Id };
        await taskRepo.AddAsync(task);
        await taskRepo.SaveChangesAsync();

        await Assert.ThrowsAsync<NotFoundException>(
            () => service.MoveTaskAsync(board.Id, task.Id, new MoveTaskDto(Guid.NewGuid())));
    }

    [Fact]
    public async Task DeleteSectionAsync_detaches_tasks_not_deletes_them()
    {
        var service = NewService(out _, out var sectionRepo, out var taskRepo);
        var board = await service.CreateAsync(new CreateWorkflowDto("Board", null, null));
        var sections = (await sectionRepo.ListAsync(s => s.WorkflowId == board.Id)).OrderBy(s => s.Sort).ToList();
        var task = new WorkTask { Title = "Việc", WorkflowId = board.Id, SectionId = sections[0].Id };
        await taskRepo.AddAsync(task);
        await taskRepo.SaveChangesAsync();

        await service.DeleteSectionAsync(board.Id, sections[0].Id);

        var reloaded = await taskRepo.GetByIdAsync(task.Id);
        Assert.NotNull(reloaded);            // việc vẫn còn
        Assert.Null(reloaded!.SectionId);    // chỉ tách khỏi cột
    }

    [Fact]
    public async Task ReorderSectionsAsync_applies_new_order()
    {
        var service = NewService(out _, out var sectionRepo, out _);
        var board = await service.CreateAsync(new CreateWorkflowDto("Board", null, null));
        var sections = (await sectionRepo.ListAsync(s => s.WorkflowId == board.Id)).OrderBy(s => s.Sort).ToList();
        var reversed = sections.Select(s => s.Id).Reverse().ToList();

        await service.ReorderSectionsAsync(board.Id, new ReorderSectionsDto(reversed));

        var detail = await service.GetBoardAsync(board.Id);
        Assert.Equal(reversed, detail.Columns.Select(c => c.Section.Id).ToList());
    }

    [Fact]
    public async Task DeleteAsync_detaches_tasks_and_removes_board_and_sections()
    {
        var service = NewService(out var repo, out var sectionRepo, out var taskRepo);
        var board = await service.CreateAsync(new CreateWorkflowDto("Board", null, null));
        var task = new WorkTask { Title = "Việc", WorkflowId = board.Id };
        await taskRepo.AddAsync(task);
        await taskRepo.SaveChangesAsync();

        await service.DeleteAsync(board.Id);

        Assert.Null(await repo.GetByIdAsync(board.Id));
        Assert.Empty(await sectionRepo.ListAsync(s => s.WorkflowId == board.Id));
        Assert.Null((await taskRepo.GetByIdAsync(task.Id))!.WorkflowId);
    }
}
