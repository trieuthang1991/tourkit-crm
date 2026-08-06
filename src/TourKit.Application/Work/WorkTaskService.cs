using FluentValidation;
using TourKit.Application.Common;
using TourKit.Application.Notifications;
using TourKit.Shared.Entities;

namespace TourKit.Application.Work;

/// <summary>
/// Công việc nội bộ (legacy Tasking) — CRUD + lọc theo người được giao/trạng thái. Không phụ thuộc
/// dịch vụ ngoài. Validate người được giao tồn tại (nếu có); resolve tên người được giao khi trả về.
/// Khi giao việc (gán/đổi người) → đẩy thông báo in-app cho người nhận (<see cref="INotificationService"/>).
/// </summary>
public sealed class WorkTaskService(
    IRepository<WorkTask> repo,
    IRepository<User> userRepo,
    IRepository<Workflow> workflowRepo,
    INotificationService notifications,
    IValidator<CreateWorkTaskDto> createValidator,
    IValidator<UpdateWorkTaskDto> updateValidator,
    TourKit.Shared.Security.ICurrentUserContext currentUser) : IWorkTaskService
{
    public async Task<PagedResult<WorkTaskDto>> ListAsync(
        int page, int size, Guid? assigneeUserId, int? status, string? q = null, int? priority = null,
        bool mineScope = false)
    {
        var kw = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        // "Của tôi" (mineScope) bám hệ cũ (Tasking): việc ĐƯỢC GIAO cho tôi HOẶC việc DO TÔI TẠO.
        // Ngoài mineScope thì giữ nguyên nghĩa cũ: lọc theo assigneeUserId truyền vào (null = tất cả).
        var me = currentUser.UserId;
        var items = await repo.ListAsync(x =>
            (mineScope
                ? (x.AssigneeUserId == me || x.CreatedByUserId == me)
                : (assigneeUserId == null || x.AssigneeUserId == assigneeUserId)) &&
            (status == null || x.Status == status) &&
            (priority == null || x.Priority == priority));

        var names = await LoadUserNamesAsync();
        var workflows = await LoadWorkflowNamesAsync();
        var ordered = items
            .Where(x => kw == null || x.Title.Contains(kw, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Status)
            .ThenByDescending(x => x.Priority)
            .ThenBy(x => x.DueDate ?? DateTimeOffset.MaxValue)
            .ToList();

        var pageItems = ordered
            .Skip((page - 1) * size)
            .Take(size)
            .Select(x => Map(x, names, workflows))
            .ToList();
        return new PagedResult<WorkTaskDto>(pageItems, ordered.Count, page, size);
    }

    public async Task<WorkTaskStatsDto> GetStatsAsync(bool mineScope = false)
    {
        // Một câu GROUP BY cho mọi bậc trạng thái, thay vì nạp cả bảng hoặc bắn nhiều câu COUNT rời.
        // mineScope: thống kê cho dashboard "Công việc của tôi" (được giao HOẶC do tôi tạo).
        var now = DateTimeOffset.UtcNow;
        var me = currentUser.UserId;
        var byStatus = await repo.CountByAsync(x => x.Status,
            x => !mineScope || x.AssigneeUserId == me || x.CreatedByUserId == me);
        int N(int s) => byStatus.GetValueOrDefault(s);

        return new WorkTaskStatsDto(
            byStatus.Values.Sum(),
            N(0), N(1), N(2), N(3),
            // "Quá hạn" không phải một bậc trạng thái → cần thêm một COUNT riêng (cùng phạm vi).
            await repo.CountAsync(x => (!mineScope || x.AssigneeUserId == me || x.CreatedByUserId == me)
                && x.DueDate != null && x.DueDate < now && x.Status != 2 && x.Status != 3));
    }

    public async Task<WorkTaskDto> CreateAsync(CreateWorkTaskDto dto)
    {
        await Validate(createValidator, dto);
        await EnsureAssigneeAsync(dto.AssigneeUserId);

        var entity = new WorkTask
        {
            // Mã Task hiển thị — idiom repo ("ORD-"/"RSV-" + guid8), an toàn, không cần bộ đếm tuần tự.
            Code = "CV-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            AssigneeUserId = dto.AssigneeUserId,
            CreatedByUserId = currentUser.UserId,   // legacy INS_UID — để lọc "Công việc của tôi"
            StartDate = dto.StartDate,
            DueDate = dto.DueDate,
            Priority = dto.Priority,
            Progress = Math.Clamp(dto.Progress, 0, 100),
            Status = dto.Status,
            RelatedOrderId = dto.RelatedOrderId,
            WorkflowId = dto.WorkflowId,
            SectionId = dto.SectionId,
        };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        await NotifyAssigneeAsync(entity);

        var names = await LoadUserNamesAsync();
        var workflows = await LoadWorkflowNamesAsync();
        return Map(entity, names, workflows);
    }

    public async Task UpdateAsync(Guid id, UpdateWorkTaskDto dto)
    {
        await Validate(updateValidator, dto);

        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        await EnsureAssigneeAsync(dto.AssigneeUserId);

        var previousAssignee = entity.AssigneeUserId;

        entity.Title = dto.Title.Trim();
        entity.Description = dto.Description?.Trim();
        entity.AssigneeUserId = dto.AssigneeUserId;
        entity.StartDate = dto.StartDate;
        entity.DueDate = dto.DueDate;
        entity.Priority = dto.Priority;
        entity.Progress = Math.Clamp(dto.Progress, 0, 100);
        entity.Status = dto.Status;
        entity.RelatedOrderId = dto.RelatedOrderId;
        entity.WorkflowId = dto.WorkflowId;
        entity.SectionId = dto.SectionId;
        repo.Update(entity);
        await repo.SaveChangesAsync();

        // Chỉ thông báo khi ĐỔI người được giao (tránh spam mỗi lần sửa).
        if (entity.AssigneeUserId != previousAssignee)
        {
            await NotifyAssigneeAsync(entity);
        }
    }

    public async Task DeleteAsync(Guid id)
    {
        var entity = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        repo.Remove(entity);
        await repo.SaveChangesAsync();
    }

    private async Task NotifyAssigneeAsync(WorkTask task)
    {
        if (task.AssigneeUserId is { } uid)
        {
            await notifications.PushAsync(uid, "Bạn được giao công việc", task.Title, "/work-tasks", "task");
        }
    }

    private async Task EnsureAssigneeAsync(Guid? assigneeUserId)
    {
        if (assigneeUserId is { } uid && !await userRepo.AnyAsync(u => u.Id == uid))
        {
            throw new ValidationAppException("Người được giao không tồn tại.");
        }
    }

    private async Task<Dictionary<Guid, string>> LoadUserNamesAsync()
    {
        var users = await userRepo.ListAsync();
        return users.ToDictionary(u => u.Id, u => u.FullName);
    }

    // Danh mục board nhỏ theo tenant — nạp một lần để tra tên "Dự án" (bám cột staging), không N+1.
    private async Task<Dictionary<Guid, string>> LoadWorkflowNamesAsync()
    {
        var flows = await workflowRepo.ListAsync();
        return flows.ToDictionary(w => w.Id, w => w.Name);
    }

    private static async Task Validate<T>(IValidator<T> validator, T dto)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }
    }

    private static WorkTaskDto Map(WorkTask x, Dictionary<Guid, string> names, Dictionary<Guid, string> workflows) => new(
        x.Id, x.Title, x.Description, x.AssigneeUserId,
        x.AssigneeUserId is { } uid && names.TryGetValue(uid, out var n) ? n : null,
        x.DueDate, x.Priority, x.Status, x.RelatedOrderId, x.WorkflowId, x.SectionId,
        x.CreatedByUserId,
        x.CreatedByUserId is { } cid && names.TryGetValue(cid, out var cn) ? cn : null,
        x.WorkflowId is { } wid && workflows.TryGetValue(wid, out var wn) ? wn : null,
        x.Code, x.StartDate, x.Progress);
}
