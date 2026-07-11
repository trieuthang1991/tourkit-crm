namespace TourKit.Application.Admin;

public sealed record UserListDto(
    Guid Id, string Email, string FullName, bool IsActive,
    Guid? DepartmentId, string? DepartmentName, Guid? PositionId, string? PositionName);

/// <summary>Gán cơ cấu tổ chức cho user (null = bỏ gán).</summary>
public sealed record AssignUserOrgDto(Guid? DepartmentId, Guid? PositionId);
