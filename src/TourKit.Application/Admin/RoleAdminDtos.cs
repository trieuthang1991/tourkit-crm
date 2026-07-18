namespace TourKit.Application.Admin;

/// <summary>Một quyền trong catalog. <c>Group</c> lấy từ nhóm hiển thị của catalog (không phải prefix code).</summary>
public sealed record PermissionDto(Guid Id, string Code, string Name, string Group);

/// <summary>Dòng danh sách vai trò kèm số quyền + số user đang gán.</summary>
public sealed record RoleListDto(Guid Id, string Name, int PermissionCount, int UserCount);

/// <summary>Chi tiết vai trò: id, tên, danh sách quyền được gán.</summary>
public sealed record RoleDetailDto(Guid Id, string Name, IReadOnlyList<Guid> PermissionIds);

/// <summary>Body HTTP tạo vai trò.</summary>
public sealed record CreateRoleRequest(string Name);

/// <summary>Body HTTP cập nhật vai trò: đổi tên + thay TOÀN BỘ tập quyền.</summary>
public sealed record UpdateRoleRequest(string Name, IReadOnlyList<Guid>? PermissionIds);
