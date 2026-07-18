namespace TourKit.Application.Admin;

public sealed record UserListDto(
    Guid Id, string Email, string FullName, bool IsActive,
    Guid? DepartmentId, string? DepartmentName, Guid? PositionId, string? PositionName,
    Guid? RoleId, string? RoleName);

/// <summary>Gán cơ cấu tổ chức cho user (null = bỏ gán).</summary>
public sealed record AssignUserOrgDto(Guid? DepartmentId, Guid? PositionId);

/// <summary>Body HTTP tạo user — mật khẩu thô (controller hash trước khi gọi service).</summary>
public sealed record CreateUserRequest(
    string Email, string FullName, string Password,
    Guid? DepartmentId, Guid? PositionId, Guid? RoleId, bool IsActive = true);

/// <summary>Đầu vào tạo user cho service — mật khẩu ĐÃ hash (giữ IPasswordHasher ở tầng Api).</summary>
public sealed record CreateUserData(
    string Email, string FullName, string PasswordHash,
    Guid? DepartmentId, Guid? PositionId, Guid? RoleId, bool IsActive);

/// <summary>Body HTTP cập nhật hồ sơ + vai trò + trạng thái khoá của user.</summary>
public sealed record UpdateUserRequest(
    string FullName, Guid? DepartmentId, Guid? PositionId, Guid? RoleId, bool IsActive);

/// <summary>Body HTTP admin đặt lại mật khẩu.</summary>
public sealed record ResetPasswordRequest(string NewPassword);
