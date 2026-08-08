using System.ComponentModel.DataAnnotations;
using TourKit.Application.Auth;
using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Admin;

/// <summary>
/// Quản lý user trong tenant: liệt kê + tạo/sửa/khoá + đặt lại mật khẩu + gán cơ cấu tổ chức và vai trò.
/// Mật khẩu được hash ở tầng Api (IPasswordHasher) rồi truyền xuống — service chỉ nhận PasswordHash.
/// Bảng nối UserRole được thay thế qua <see cref="IRbacStore"/> (hard-delete, tránh đụng unique index).
/// </summary>
public sealed class UserAdminService(
    IRepository<User> userRepo,
    IRepository<Department> departmentRepo,
    IRepository<Position> positionRepo,
    IRepository<Role> roleRepo,
    IRepository<UserRole> userRoleRepo,
    IRbacStore rbac,
    IUserIdentityStore identity) : IUserAdminService
{
    public async Task<IReadOnlyList<UserListDto>> ListAsync()
    {
        var users = await userRepo.ListAsync();
        var lookups = await LoadLookupsAsync();
        return users.OrderBy(u => u.FullName).Select(u => Map(u, lookups)).ToList();
    }

    public async Task<UserListDto> AssignOrgAsync(Guid userId, AssignUserOrgDto dto)
    {
        var user = await userRepo.GetByIdAsync(userId) ?? throw new NotFoundException();
        await ValidateOrgAsync(dto.DepartmentId, dto.PositionId);

        user.DepartmentId = dto.DepartmentId;
        user.PositionId = dto.PositionId;
        userRepo.Update(user);
        await userRepo.SaveChangesAsync();

        return Map(user, await LoadLookupsAsync());
    }

    public async Task<UserListDto> CreateAsync(CreateUserData data)
    {
        var email = (data.Email ?? string.Empty).Trim();
        var fullName = (data.FullName ?? string.Empty).Trim();

        if (!new EmailAddressAttribute().IsValid(email))
        {
            throw new ValidationAppException("Email không hợp lệ.");
        }
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ValidationAppException("Họ tên không được trống.");
        }
        if (string.IsNullOrWhiteSpace(data.PasswordHash))
        {
            throw new ValidationAppException("Mật khẩu không được trống.");
        }
        if (await identity.EmailExistsAsync(email))
        {
            throw new ConflictException("Email đã tồn tại trong hệ thống.");
        }
        await ValidateOrgAsync(data.DepartmentId, data.PositionId);
        await ValidateRoleAsync(data.RoleId);

        var user = new User
        {
            Email = email,
            FullName = fullName,
            PasswordHash = data.PasswordHash,
            IsActive = data.IsActive,
            DepartmentId = data.DepartmentId,
            PositionId = data.PositionId,
        };
        await userRepo.AddAsync(user);
        await userRepo.SaveChangesAsync();

        if (data.RoleId is { } roleId)
        {
            await userRoleRepo.AddAsync(new UserRole { UserId = user.Id, RoleId = roleId });
            await userRoleRepo.SaveChangesAsync();
        }

        return Map(user, await LoadLookupsAsync());
    }

    public async Task<UserListDto> UpdateAsync(Guid userId, UpdateUserRequest dto)
    {
        var user = await userRepo.GetByIdAsync(userId) ?? throw new NotFoundException();
        var fullName = (dto.FullName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ValidationAppException("Họ tên không được trống.");
        }
        await ValidateOrgAsync(dto.DepartmentId, dto.PositionId);
        await ValidateRoleAsync(dto.RoleId);

        user.FullName = fullName;
        user.DepartmentId = dto.DepartmentId;
        user.PositionId = dto.PositionId;
        user.IsActive = dto.IsActive;
        userRepo.Update(user);
        await userRepo.SaveChangesAsync();

        // Thay toàn bộ vai trò user về đúng RoleId (null = bỏ hết).
        var desired = dto.RoleId is { } rid ? new[] { rid } : Array.Empty<Guid>();
        await rbac.ReplaceUserRolesAsync(userId, desired);

        return Map(user, await LoadLookupsAsync());
    }

    public async Task ResetPasswordAsync(Guid userId, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ValidationAppException("Mật khẩu không được trống.");
        }
        var user = await userRepo.GetByIdAsync(userId) ?? throw new NotFoundException();
        user.PasswordHash = passwordHash;
        userRepo.Update(user);
        await userRepo.SaveChangesAsync();
    }

    public async Task<UserListDto> ToggleActiveAsync(Guid userId)
    {
        var user = await userRepo.GetByIdAsync(userId) ?? throw new NotFoundException();
        user.IsActive = !user.IsActive;
        userRepo.Update(user);
        await userRepo.SaveChangesAsync();
        return Map(user, await LoadLookupsAsync());
    }

    private async Task ValidateOrgAsync(Guid? departmentId, Guid? positionId)
    {
        if (departmentId is { } deptId && !await departmentRepo.AnyAsync(d => d.Id == deptId))
        {
            throw new ValidationAppException("Phòng ban không tồn tại.");
        }
        if (positionId is { } posId && !await positionRepo.AnyAsync(p => p.Id == posId))
        {
            throw new ValidationAppException("Chức vụ không tồn tại.");
        }
    }

    private async Task ValidateRoleAsync(Guid? roleId)
    {
        if (roleId is { } id && !await roleRepo.AnyAsync(r => r.Id == id))
        {
            throw new ValidationAppException("Vai trò không tồn tại.");
        }
    }

    private async Task<Lookups> LoadLookupsAsync()
    {
        var departments = (await departmentRepo.ListAsync()).ToDictionary(d => d.Id, d => d.Name);
        var positions = (await positionRepo.ListAsync()).ToDictionary(p => p.Id, p => p.Name);
        var roles = (await roleRepo.ListAsync()).ToDictionary(r => r.Id, r => r.Name);
        // Một user có thể có nhiều vai trò; contract mô hình 1 vai trò → lấy vai trò đầu tiên.
        var userRole = (await userRoleRepo.ListAsync())
            .GroupBy(ur => ur.UserId)
            .ToDictionary(g => g.Key, g => g.First().RoleId);
        return new Lookups(departments, positions, roles, userRole);
    }

    private static UserListDto Map(User u, Lookups l)
    {
        Guid? roleId = l.UserRole.TryGetValue(u.Id, out var rid) ? rid : null;
        string? roleName = roleId is { } r && l.Roles.TryGetValue(r, out var rn) ? rn : null;
        return new UserListDto(
            u.Id, u.Email, u.FullName, u.IsActive,
            u.DepartmentId, u.DepartmentId is { } d && l.Departments.TryGetValue(d, out var dn) ? dn : null,
            u.PositionId, u.PositionId is { } p && l.Positions.TryGetValue(p, out var pn) ? pn : null,
            roleId, roleName);
    }

    private sealed record Lookups(
        Dictionary<Guid, string> Departments,
        Dictionary<Guid, string> Positions,
        Dictionary<Guid, string> Roles,
        Dictionary<Guid, Guid> UserRole);
}
