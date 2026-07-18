namespace TourKit.Application.Admin;

public interface IRoleAdminService
{
    /// <summary>Toàn bộ catalog quyền (global) — dùng cho màn gán quyền.</summary>
    Task<IReadOnlyList<PermissionDto>> ListPermissionsAsync();

    Task<IReadOnlyList<RoleListDto>> ListRolesAsync();
    Task<RoleDetailDto> GetRoleAsync(Guid id);

    Task<RoleDetailDto> CreateAsync(CreateRoleRequest dto);

    /// <summary>Đổi tên + thay toàn bộ tập quyền của vai trò.</summary>
    Task<RoleDetailDto> UpdateAsync(Guid id, UpdateRoleRequest dto);

    /// <summary>Xoá vai trò (chặn nếu là Admin mặc định hoặc còn user đang gán).</summary>
    Task DeleteAsync(Guid id);
}
