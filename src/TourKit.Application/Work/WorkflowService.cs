using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Work;

/// <summary>
/// Board Kanban cấu hình động (legacy <c>Workflow</c>/<c>SectionWork</c>). Người dùng tự tạo board,
/// tự định nghĩa các cột (trạng thái) như DỮ LIỆU, rồi kéo thẻ việc (<see cref="WorkTask"/>) giữa các cột —
/// không hard-code state/transition. Board mới được gieo sẵn 3 cột mặc định (Cần làm/Đang làm/Hoàn thành).
/// </summary>
public sealed class WorkflowService(
    IRepository<Workflow> repo,
    IRepository<WorkflowSection> sectionRepo,
    IRepository<WorkTask> taskRepo,
    IRepository<User> userRepo) : IWorkflowService
{
    private const int Archived = 1;

    public async Task<IReadOnlyList<WorkflowDto>> ListAsync()
    {
        var boards = await repo.ListAsync();
        var sections = await sectionRepo.ListAsync();
        var tasks = await taskRepo.ListAsync(t => t.WorkflowId != null);

        return boards
            .OrderBy(b => b.Status)
            .ThenByDescending(b => b.CreatedAt)
            .Select(b => new WorkflowDto(
                b.Id, b.Name, b.StartDate, b.EndDate, b.Status,
                sections.Count(s => s.WorkflowId == b.Id),
                tasks.Count(t => t.WorkflowId == b.Id)))
            .ToList();
    }

    public async Task<WorkflowBoardDto> GetBoardAsync(Guid id)
    {
        var board = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        var sections = (await sectionRepo.ListAsync(s => s.WorkflowId == id)).OrderBy(s => s.Sort).ToList();
        var tasks = await taskRepo.ListAsync(t => t.WorkflowId == id);
        var names = await LoadUserNamesAsync();

        var columns = sections.Select(s => new BoardColumnDto(
                MapSection(s),
                tasks.Where(t => t.SectionId == s.Id)
                    .OrderByDescending(t => t.Priority)
                    .ThenBy(t => t.DueDate ?? DateTimeOffset.MaxValue)
                    .Select(t => MapTask(t, names))
                    .ToList()))
            .ToList();

        return new WorkflowBoardDto(board.Id, board.Name, board.Status, columns);
    }

    public async Task<WorkflowDto> CreateAsync(CreateWorkflowDto dto)
    {
        EnsureName(dto.Name);
        var board = new Workflow
        {
            Name = dto.Name.Trim(),
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Status = 0,
        };
        await repo.AddAsync(board);
        await repo.SaveChangesAsync();

        // Gieo cột mặc định — người dùng có thể đổi tên/thêm/xoá sau.
        string[] defaults = ["Cần làm", "Đang làm", "Hoàn thành"];
        for (var i = 0; i < defaults.Length; i++)
        {
            await sectionRepo.AddAsync(new WorkflowSection
            {
                WorkflowId = board.Id,
                Name = defaults[i],
                Sort = i,
            });
        }

        await sectionRepo.SaveChangesAsync();

        return new WorkflowDto(board.Id, board.Name, board.StartDate, board.EndDate, board.Status, defaults.Length, 0);
    }

    public async Task UpdateAsync(Guid id, UpdateWorkflowDto dto)
    {
        EnsureName(dto.Name);
        var board = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        board.Name = dto.Name.Trim();
        board.StartDate = dto.StartDate;
        board.EndDate = dto.EndDate;
        board.Status = dto.Status;
        repo.Update(board);
        await repo.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var board = await repo.GetByIdAsync(id) ?? throw new NotFoundException();

        // Gỡ liên kết thẻ việc (giữ lại việc, chỉ tách khỏi board) rồi xoá cột + board.
        var tasks = await taskRepo.ListAsync(t => t.WorkflowId == id);
        foreach (var t in tasks)
        {
            t.WorkflowId = null;
            t.SectionId = null;
            taskRepo.Update(t);
        }

        var sections = await sectionRepo.ListAsync(s => s.WorkflowId == id);
        foreach (var s in sections)
        {
            sectionRepo.Remove(s);
        }

        repo.Remove(board);
        await repo.SaveChangesAsync();
    }

    public async Task<WorkflowSectionDto> AddSectionAsync(Guid workflowId, CreateSectionDto dto)
    {
        EnsureName(dto.Name);
        _ = await repo.GetByIdAsync(workflowId) ?? throw new NotFoundException();

        var existing = await sectionRepo.ListAsync(s => s.WorkflowId == workflowId);
        var section = new WorkflowSection
        {
            WorkflowId = workflowId,
            Name = dto.Name.Trim(),
            Color = dto.Color?.Trim(),
            Icon = dto.Icon?.Trim(),
            Sort = existing.Count == 0 ? 0 : existing.Max(s => s.Sort) + 1,
        };
        await sectionRepo.AddAsync(section);
        await sectionRepo.SaveChangesAsync();
        return MapSection(section);
    }

    public async Task UpdateSectionAsync(Guid workflowId, Guid sectionId, UpdateSectionDto dto)
    {
        EnsureName(dto.Name);
        var section = await GetOwnedSectionAsync(workflowId, sectionId);
        if (!section.AllowUpdate)
        {
            throw new ValidationAppException("Cột này không cho phép sửa.");
        }

        section.Name = dto.Name.Trim();
        section.Color = dto.Color?.Trim();
        section.Icon = dto.Icon?.Trim();
        sectionRepo.Update(section);
        await sectionRepo.SaveChangesAsync();
    }

    public async Task DeleteSectionAsync(Guid workflowId, Guid sectionId)
    {
        var section = await GetOwnedSectionAsync(workflowId, sectionId);
        if (!section.AllowDelete)
        {
            throw new ValidationAppException("Cột này không cho phép xoá.");
        }

        // Tách thẻ việc khỏi cột bị xoá (không xoá việc).
        var tasks = await taskRepo.ListAsync(t => t.SectionId == sectionId);
        foreach (var t in tasks)
        {
            t.SectionId = null;
            taskRepo.Update(t);
        }

        sectionRepo.Remove(section);
        await sectionRepo.SaveChangesAsync();
    }

    public async Task ReorderSectionsAsync(Guid workflowId, ReorderSectionsDto dto)
    {
        var sections = await sectionRepo.ListAsync(s => s.WorkflowId == workflowId);
        var byId = sections.ToDictionary(s => s.Id);
        var order = 0;
        foreach (var id in dto.SectionIds)
        {
            if (byId.TryGetValue(id, out var section))
            {
                section.Sort = order++;
                sectionRepo.Update(section);
            }
        }

        await sectionRepo.SaveChangesAsync();
    }

    public async Task MoveTaskAsync(Guid workflowId, Guid taskId, MoveTaskDto dto)
    {
        _ = await GetOwnedSectionAsync(workflowId, dto.SectionId);
        var task = await taskRepo.GetByIdAsync(taskId) ?? throw new NotFoundException();

        task.WorkflowId = workflowId;
        task.SectionId = dto.SectionId;
        taskRepo.Update(task);
        await taskRepo.SaveChangesAsync();
    }

    private async Task<WorkflowSection> GetOwnedSectionAsync(Guid workflowId, Guid sectionId)
    {
        var section = await sectionRepo.GetByIdAsync(sectionId) ?? throw new NotFoundException();
        if (section.WorkflowId != workflowId)
        {
            throw new NotFoundException();
        }

        return section;
    }

    private static void EnsureName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationAppException("Tên không được trống.");
        }
    }

    private async Task<Dictionary<Guid, string>> LoadUserNamesAsync()
    {
        var users = await userRepo.ListAsync();
        return users.ToDictionary(u => u.Id, u => u.FullName);
    }

    private static WorkflowSectionDto MapSection(WorkflowSection s) => new(
        s.Id, s.WorkflowId, s.Name, s.Sort, s.Color, s.Icon, s.AllowUpdate, s.AllowDelete);

    private static WorkTaskDto MapTask(WorkTask x, Dictionary<Guid, string> names) => new(
        x.Id, x.Title, x.Description, x.AssigneeUserId,
        x.AssigneeUserId is { } uid && names.TryGetValue(uid, out var n) ? n : null,
        x.DueDate, x.Priority, x.Status, x.RelatedOrderId, x.WorkflowId, x.SectionId);
}
