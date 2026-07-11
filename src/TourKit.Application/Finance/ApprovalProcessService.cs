using TourKit.Application.Common;
using TourKit.Shared.Entities;
using TourKit.Shared.Enums;

namespace TourKit.Application.Finance;

/// <summary>
/// Quy trình duyệt CẤU HÌNH được (legacy <c>ApprovalProcess</c>/<c>ApprovalStep</c>/<c>ApprovalStepUser</c>).
/// Admin dựng template: các bước theo <see cref="Position"/>, mỗi bước gán người duyệt cụ thể — như dữ liệu,
/// không hard-code số cấp. Lớp ĐỊNH NGHĨA, tách khỏi luồng duyệt cụ thể theo phiếu (Payment/ReceiptApproval).
/// </summary>
public sealed class ApprovalProcessService(
    IRepository<ApprovalProcess> repo,
    IRepository<ApprovalProcessStep> stepRepo,
    IRepository<ApprovalProcessStepUser> stepUserRepo,
    IRepository<Position> positionRepo,
    IRepository<User> userRepo) : IApprovalProcessService
{
    public async Task<IReadOnlyList<ApprovalProcessDto>> ListAsync()
    {
        var items = await repo.ListAsync();
        var steps = await stepRepo.ListAsync();
        return items
            .OrderBy(p => p.Status)
            .ThenBy(p => p.Name)
            .Select(p => new ApprovalProcessDto(
                p.Id, p.Name, (int)p.Method, p.Status, steps.Count(s => s.ApprovalProcessId == p.Id)))
            .ToList();
    }

    public async Task<ApprovalProcessDetailDto> GetAsync(Guid id)
    {
        var process = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        var steps = (await stepRepo.ListAsync(s => s.ApprovalProcessId == id)).OrderBy(s => s.StepOrder).ToList();
        var stepIds = steps.Select(s => s.Id).ToHashSet();
        var stepUsers = (await stepUserRepo.ListAsync()).Where(su => stepIds.Contains(su.ApprovalProcessStepId)).ToList();
        var positionNames = await LoadPositionNamesAsync();
        var userNames = await LoadUserNamesAsync();

        var stepDtos = steps.Select(s =>
        {
            var users = stepUsers.Where(su => su.ApprovalProcessStepId == s.Id).ToList();
            return new ApprovalProcessStepDto(
                s.Id, s.StepOrder, s.PositionId,
                positionNames.GetValueOrDefault(s.PositionId),
                users.Select(u => u.UserId).ToList(),
                users.Select(u => userNames.GetValueOrDefault(u.UserId, "?")).ToList());
        }).ToList();

        return new ApprovalProcessDetailDto(process.Id, process.Name, (int)process.Method, process.Status, stepDtos);
    }

    public async Task<ApprovalProcessDto> CreateAsync(CreateApprovalProcessDto dto)
    {
        EnsureName(dto.Name);
        var process = new ApprovalProcess
        {
            Name = dto.Name.Trim(),
            Method = ParseMethod(dto.Method),
            Status = 0,
        };
        await repo.AddAsync(process);
        await repo.SaveChangesAsync();
        return new ApprovalProcessDto(process.Id, process.Name, (int)process.Method, process.Status, 0);
    }

    public async Task UpdateAsync(Guid id, UpdateApprovalProcessDto dto)
    {
        EnsureName(dto.Name);
        var process = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        process.Name = dto.Name.Trim();
        process.Method = ParseMethod(dto.Method);
        process.Status = dto.Status;
        repo.Update(process);
        await repo.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var process = await repo.GetByIdAsync(id) ?? throw new NotFoundException();
        var steps = await stepRepo.ListAsync(s => s.ApprovalProcessId == id);
        var stepIds = steps.Select(s => s.Id).ToHashSet();
        var users = (await stepUserRepo.ListAsync()).Where(su => stepIds.Contains(su.ApprovalProcessStepId)).ToList();

        foreach (var u in users)
        {
            stepUserRepo.Remove(u);
        }

        foreach (var s in steps)
        {
            stepRepo.Remove(s);
        }

        repo.Remove(process);
        await repo.SaveChangesAsync();
    }

    public async Task<ApprovalProcessStepDto> AddStepAsync(Guid processId, AddStepDto dto)
    {
        _ = await repo.GetByIdAsync(processId) ?? throw new NotFoundException();
        if (!await positionRepo.AnyAsync(p => p.Id == dto.PositionId))
        {
            throw new ValidationAppException("Chức vụ không tồn tại.");
        }

        var existing = await stepRepo.ListAsync(s => s.ApprovalProcessId == processId);
        var step = new ApprovalProcessStep
        {
            ApprovalProcessId = processId,
            PositionId = dto.PositionId,
            StepOrder = existing.Count == 0 ? 1 : existing.Max(s => s.StepOrder) + 1,
        };
        await stepRepo.AddAsync(step);
        await stepRepo.SaveChangesAsync();

        var names = await LoadPositionNamesAsync();
        return new ApprovalProcessStepDto(step.Id, step.StepOrder, step.PositionId,
            names.GetValueOrDefault(step.PositionId), [], []);
    }

    public async Task DeleteStepAsync(Guid processId, Guid stepId)
    {
        var step = await GetOwnedStepAsync(processId, stepId);
        var users = await stepUserRepo.ListAsync(su => su.ApprovalProcessStepId == stepId);
        foreach (var u in users)
        {
            stepUserRepo.Remove(u);
        }

        stepRepo.Remove(step);
        await stepRepo.SaveChangesAsync();
    }

    public async Task ReorderStepsAsync(Guid processId, ReorderStepsDto dto)
    {
        var steps = await stepRepo.ListAsync(s => s.ApprovalProcessId == processId);
        var byId = steps.ToDictionary(s => s.Id);
        var order = 1;
        foreach (var id in dto.StepIds)
        {
            if (byId.TryGetValue(id, out var step))
            {
                step.StepOrder = order++;
                stepRepo.Update(step);
            }
        }

        await stepRepo.SaveChangesAsync();
    }

    public async Task SetStepUsersAsync(Guid processId, Guid stepId, SetStepUsersDto dto)
    {
        _ = await GetOwnedStepAsync(processId, stepId);

        var distinctUserIds = dto.UserIds.Distinct().ToList();
        foreach (var uid in distinctUserIds)
        {
            if (!await userRepo.AnyAsync(u => u.Id == uid))
            {
                throw new ValidationAppException("Người duyệt không tồn tại.");
            }
        }

        // Thay thế toàn bộ danh sách người duyệt của bước.
        var current = await stepUserRepo.ListAsync(su => su.ApprovalProcessStepId == stepId);
        foreach (var su in current)
        {
            stepUserRepo.Remove(su);
        }

        foreach (var uid in distinctUserIds)
        {
            await stepUserRepo.AddAsync(new ApprovalProcessStepUser { ApprovalProcessStepId = stepId, UserId = uid });
        }

        await stepUserRepo.SaveChangesAsync();
    }

    private async Task<ApprovalProcessStep> GetOwnedStepAsync(Guid processId, Guid stepId)
    {
        var step = await stepRepo.GetByIdAsync(stepId) ?? throw new NotFoundException();
        if (step.ApprovalProcessId != processId)
        {
            throw new NotFoundException();
        }

        return step;
    }

    private static void EnsureName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationAppException("Tên quy trình không được trống.");
        }
    }

    private static ApprovalMethod ParseMethod(int method) =>
        Enum.IsDefined(typeof(ApprovalMethod), method)
            ? (ApprovalMethod)method
            : throw new ValidationAppException("Phương thức duyệt không hợp lệ.");

    private async Task<Dictionary<Guid, string>> LoadPositionNamesAsync()
    {
        var positions = await positionRepo.ListAsync();
        return positions.ToDictionary(p => p.Id, p => p.Name);
    }

    private async Task<Dictionary<Guid, string>> LoadUserNamesAsync()
    {
        var users = await userRepo.ListAsync();
        return users.ToDictionary(u => u.Id, u => u.FullName);
    }
}
