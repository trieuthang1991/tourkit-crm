using Microsoft.EntityFrameworkCore;
using TourKit.Application.Admin;
using TourKit.Infrastructure.Persistence;
using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Admin;

/// <summary>
/// Ghi bảng nối RBAC bằng HARD delete qua <c>AppDbContext</c> (repo generic chỉ soft-delete → đụng
/// unique index khi gán lại cùng cặp). Query filter tự bám tenant hiện tại + ẩn soft-deleted; interceptor
/// gán TenantId cho bản ghi mới. Vì chỉ hard-delete nên không để lại bản ghi soft-deleted gây trùng khoá.
/// </summary>
public sealed class RbacStore(AppDbContext db) : IRbacStore
{
    public async Task ReplaceRolePermissionsAsync(Guid roleId, IReadOnlyCollection<Guid> permissionIds)
    {
        var desired = permissionIds.Distinct().ToHashSet();
        var existing = await db.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();

        var toRemove = existing.Where(rp => !desired.Contains(rp.PermissionId)).ToList();
        db.RolePermissions.RemoveRange(toRemove);

        var have = existing.Select(rp => rp.PermissionId).ToHashSet();
        foreach (var pid in desired.Where(d => !have.Contains(d)))
        {
            db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = pid });
        }

        await db.SaveChangesAsync();
    }

    public async Task ReplaceUserRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds)
    {
        var desired = roleIds.Distinct().ToHashSet();
        var existing = await db.UserRoles.Where(ur => ur.UserId == userId).ToListAsync();

        var toRemove = existing.Where(ur => !desired.Contains(ur.RoleId)).ToList();
        db.UserRoles.RemoveRange(toRemove);

        var have = existing.Select(ur => ur.RoleId).ToHashSet();
        foreach (var rid in desired.Where(d => !have.Contains(d)))
        {
            db.UserRoles.Add(new UserRole { UserId = userId, RoleId = rid });
        }

        await db.SaveChangesAsync();
    }

    public async Task DeleteRoleCascadeAsync(Guid roleId)
    {
        var perms = await db.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
        db.RolePermissions.RemoveRange(perms);

        var role = await db.Roles.FirstOrDefaultAsync(r => r.Id == roleId);
        if (role is not null)
        {
            db.Roles.Remove(role);   // hard delete: tên vai trò unique/tenant → tránh chặn khi tạo lại
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Cùng phép nối như lúc đăng nhập (UserRoles → RolePermissions → Permissions), chỉ đảo chiều:
    /// từ mã quyền ra danh sách user. Toàn bộ chạy Ở SQL, không nạp bảng về lọc trong bộ nhớ.
    /// </summary>
    public async Task<IReadOnlyList<Guid>> UserIdsWithPermissionAsync(string permissionCode)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            return [];
        }

        return await db.Permissions
            .Where(p => p.Code == permissionCode)
            .Join(db.RolePermissions, p => p.Id, rp => rp.PermissionId, (p, rp) => rp.RoleId)
            .Join(db.UserRoles, roleId => roleId, ur => ur.RoleId, (roleId, ur) => ur.UserId)
            .Distinct()
            .ToListAsync();
    }
}
