using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Admin;

/// <summary>
/// Quản lý user trong tenant: liệt kê + gán cơ cấu tổ chức (phòng ban/chức vụ). KHÔNG tạo/xoá user
/// (user sinh qua provisioning/đăng ký). Chỉ đọc + gán DepartmentId/PositionId (validate tồn tại).
/// </summary>
public sealed class UserAdminService(
    IRepository<User> userRepo,
    IRepository<Department> departmentRepo,
    IRepository<Position> positionRepo) : IUserAdminService
{
    public async Task<IReadOnlyList<UserListDto>> ListAsync()
    {
        var users = await userRepo.ListAsync();
        var departments = (await departmentRepo.ListAsync()).ToDictionary(d => d.Id, d => d.Name);
        var positions = (await positionRepo.ListAsync()).ToDictionary(p => p.Id, p => p.Name);

        return users
            .OrderBy(u => u.FullName)
            .Select(u => Map(u, departments, positions))
            .ToList();
    }

    public async Task<UserListDto> AssignOrgAsync(Guid userId, AssignUserOrgDto dto)
    {
        var user = await userRepo.GetByIdAsync(userId) ?? throw new NotFoundException();

        if (dto.DepartmentId is { } deptId && !await departmentRepo.AnyAsync(d => d.Id == deptId))
        {
            throw new ValidationAppException("Phòng ban không tồn tại.");
        }

        if (dto.PositionId is { } posId && !await positionRepo.AnyAsync(p => p.Id == posId))
        {
            throw new ValidationAppException("Chức vụ không tồn tại.");
        }

        user.DepartmentId = dto.DepartmentId;
        user.PositionId = dto.PositionId;
        userRepo.Update(user);
        await userRepo.SaveChangesAsync();

        var departments = (await departmentRepo.ListAsync()).ToDictionary(d => d.Id, d => d.Name);
        var positions = (await positionRepo.ListAsync()).ToDictionary(p => p.Id, p => p.Name);
        return Map(user, departments, positions);
    }

    private static UserListDto Map(User u, Dictionary<Guid, string> departments, Dictionary<Guid, string> positions) => new(
        u.Id, u.Email, u.FullName, u.IsActive,
        u.DepartmentId, u.DepartmentId is { } d && departments.TryGetValue(d, out var dn) ? dn : null,
        u.PositionId, u.PositionId is { } p && positions.TryGetValue(p, out var pn) ? pn : null);
}
