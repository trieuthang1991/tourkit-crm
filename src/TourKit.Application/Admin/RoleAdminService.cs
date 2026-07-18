using TourKit.Application.Common;
using TourKit.Shared.Entities;

namespace TourKit.Application.Admin;

/// <summary>
/// Quản lý vai trò + gán quyền trong tenant. CRUD vai trò qua <see cref="IRepository{T}"/>; thay tập
/// quyền / xoá cascade qua <see cref="IRbacStore"/> (hard-delete bảng nối). Catalog quyền là global.
/// </summary>
public sealed class RoleAdminService(
    IRepository<Role> roleRepo,
    IRepository<Permission> permissionRepo,
    IRepository<RolePermission> rolePermissionRepo,
    IRepository<UserRole> userRoleRepo,
    IRbacStore rbac) : IRoleAdminService
{
    /// <summary>Tên vai trò sinh bởi provisioning (đủ quyền) — chặn xoá.</summary>
    private const string ProvisionedAdminRole = "Admin";

    public async Task<IReadOnlyList<PermissionDto>> ListPermissionsAsync()
    {
        var perms = await permissionRepo.ListAsync();
        return perms
            .OrderBy(p => p.Group, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.Code, StringComparer.OrdinalIgnoreCase)
            .Select(p => new PermissionDto(p.Id, p.Code, Humanize(p.Code), p.Group))
            .ToList();
    }

    public async Task<IReadOnlyList<RoleListDto>> ListRolesAsync()
    {
        var roles = await roleRepo.ListAsync();
        var permCounts = (await rolePermissionRepo.ListAsync())
            .GroupBy(rp => rp.RoleId).ToDictionary(g => g.Key, g => g.Count());
        var userCounts = (await userRoleRepo.ListAsync())
            .GroupBy(ur => ur.RoleId).ToDictionary(g => g.Key, g => g.Count());

        return roles
            .OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .Select(r => new RoleListDto(
                r.Id, r.Name,
                permCounts.GetValueOrDefault(r.Id, 0),
                userCounts.GetValueOrDefault(r.Id, 0)))
            .ToList();
    }

    public async Task<RoleDetailDto> GetRoleAsync(Guid id)
    {
        var role = await roleRepo.GetByIdAsync(id) ?? throw new NotFoundException();
        var permIds = (await rolePermissionRepo.ListAsync(rp => rp.RoleId == id))
            .Select(rp => rp.PermissionId).ToList();
        return new RoleDetailDto(role.Id, role.Name, permIds);
    }

    public async Task<RoleDetailDto> CreateAsync(CreateRoleRequest dto)
    {
        var name = NormalizeName(dto.Name);
        if (await roleRepo.AnyAsync(r => r.Name == name))
        {
            throw new ConflictException("Tên vai trò đã tồn tại.");
        }

        var role = new Role { Name = name };
        await roleRepo.AddAsync(role);
        await roleRepo.SaveChangesAsync();
        return new RoleDetailDto(role.Id, role.Name, []);
    }

    public async Task<RoleDetailDto> UpdateAsync(Guid id, UpdateRoleRequest dto)
    {
        var role = await roleRepo.GetByIdAsync(id) ?? throw new NotFoundException();
        var name = NormalizeName(dto.Name);
        if (await roleRepo.AnyAsync(r => r.Name == name && r.Id != id))
        {
            throw new ConflictException("Tên vai trò đã tồn tại.");
        }

        var permIds = (dto.PermissionIds ?? []).Distinct().ToList();
        if (permIds.Count > 0)
        {
            var found = await permissionRepo.ListAsync(p => permIds.Contains(p.Id));
            if (found.Count != permIds.Count)
            {
                throw new ValidationAppException("Danh sách quyền chứa quyền không hợp lệ.");
            }
        }

        role.Name = name;
        roleRepo.Update(role);
        await roleRepo.SaveChangesAsync();

        await rbac.ReplaceRolePermissionsAsync(id, permIds);
        return await GetRoleAsync(id);
    }

    public async Task DeleteAsync(Guid id)
    {
        var role = await roleRepo.GetByIdAsync(id) ?? throw new NotFoundException();
        if (string.Equals(role.Name, ProvisionedAdminRole, StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("Không thể xoá vai trò Admin mặc định.");
        }
        if (await userRoleRepo.AnyAsync(ur => ur.RoleId == id))
        {
            throw new ConflictException("Vai trò đang được gán cho người dùng — không thể xoá.");
        }

        await rbac.DeleteRoleCascadeAsync(id);
    }

    private static string NormalizeName(string? raw)
    {
        var name = (raw ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationAppException("Tên vai trò không được trống.");
        }
        if (name.Length > 100)
        {
            throw new ValidationAppException("Tên vai trò tối đa 100 ký tự.");
        }
        return name;
    }

    /// <summary>"customer.view" → "Customer View" (nhãn hiển thị suy ra từ code).</summary>
    private static string Humanize(string code)
    {
        var parts = code.Replace('.', ' ').Replace('_', ' ')
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', parts.Select(p => char.ToUpperInvariant(p[0]) + p[1..]));
    }
}
