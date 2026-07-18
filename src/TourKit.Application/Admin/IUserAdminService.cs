namespace TourKit.Application.Admin;

public interface IUserAdminService
{
    Task<IReadOnlyList<UserListDto>> ListAsync();
    Task<UserListDto> AssignOrgAsync(Guid userId, AssignUserOrgDto dto);

    /// <summary>Tạo user trong tenant hiện tại (mật khẩu đã hash). Gán vai trò nếu có RoleId.</summary>
    Task<UserListDto> CreateAsync(CreateUserData data);

    /// <summary>Cập nhật hồ sơ + vai trò (thay thế) + trạng thái khoá.</summary>
    Task<UserListDto> UpdateAsync(Guid userId, UpdateUserRequest dto);

    /// <summary>Admin đặt lại mật khẩu (đã hash).</summary>
    Task ResetPasswordAsync(Guid userId, string passwordHash);

    /// <summary>Khoá/mở khoá user (đảo IsActive).</summary>
    Task<UserListDto> ToggleActiveAsync(Guid userId);
}
