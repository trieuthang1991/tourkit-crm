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
    INotificationService notifications,
    IValidator<CreateWorkTaskDto> createValidator,
    IValidator<UpdateWorkTaskDto> updateValidator) : IWorkTaskService
{
    public async Task<PagedResult<WorkTaskDto>> ListAsync(
        int page, int size, Guid? assigneeUserId, int? status, string? q = null, int? priority = null)
    {
        var kw = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        var items = await repo.ListAsync(x =>
            (assigneeUserId == null || x.AssigneeUserId == assigneeUserId) &&
            (status == null || x.Status == status) &&
            (priority == null || x.Priority == priority));

        var names = await LoadUserNamesAsync();
        var ordered = items
            .Where(x => kw == null || x.Title.Contains(kw, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Status)
            .ThenByDescending(x => x.Priority)
            .ThenBy(x => x.DueDate ?? DateTimeOffset.MaxValue)
            .ToList();

        var pageItems = ordered
            .Skip((page - 1) * size)
            .Take(size)
            .Select(x => Map(x, names))
            .ToList();
        return new PagedResult<WorkTaskDto>(pageItems, ordered.Count, page, size);
    }

    public async Task<WorkTaskStatsDto> GetStatsAsync()
    {
        // Một câu GROUP BY cho mọi bậc trạng thái, thay vì nạp cả bảng hoặc bắn nhiều câu COUNT rời.
        var now = DateTimeOffset.UtcNow;
        var byStatus = await repo.CountByAsync(x => x.Status);
        int N(int s) => byStatus.GetValueOrDefault(s);

        return new WorkTaskStatsDto(
            byStatus.Values.Sum(),
            N(0), N(1), N(2), N(3),
            // "Quá hạn" không phải một bậc trạng thái → cần thêm một COUNT riêng.
            await repo.CountAsync(x => x.DueDate != null && x.DueDate < now && x.Status != 2 && x.Status != 3));
    }

    public async Task<WorkTaskDto> CreateAsync(CreateWorkTaskDto dto)
    {
        await Validate(createValidator, dto);
        await EnsureAssigneeAsync(dto.AssigneeUserId);

        var entity = new WorkTask
        {
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim(),
            AssigneeUserId = dto.AssigneeUserId,
            DueDate = dto.DueDate,
            Priority = dto.Priority,
            Status = dto.Status,
            RelatedOrderId = dto.RelatedOrderId,
            WorkflowId = dto.WorkflowId,
            SectionId = dto.SectionId,
        };
        await repo.AddAsync(entity);
        await repo.SaveChangesAsync();

        await NotifyAssigneeAsync(entity);

        var names = await LoadUserNamesAsync();
        return Map(entity, names);
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
        entity.DueDate = dto.DueDate;
        entity.Priority = dto.Priority;
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

    private static async Task Validate<T>(IValidator<T> validator, T dto)
    {
        var result = await validator.ValidateAsync(dto);
        if (!result.IsValid)
        {
            throw new ValidationAppException(result.Errors[0].ErrorMessage);
        }
    }

    private static WorkTaskDto Map(WorkTask x, Dictionary<Guid, string> names) => new(
        x.Id, x.Title, x.Description, x.AssigneeUserId,
        x.AssigneeUserId is { } uid && names.TryGetValue(uid, out var n) ? n : null,
        x.DueDate, x.Priority, x.Status, x.RelatedOrderId, x.WorkflowId, x.SectionId);
}
