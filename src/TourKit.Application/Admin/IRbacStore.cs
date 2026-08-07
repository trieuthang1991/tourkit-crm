namespace TourKit.Application.Admin;

/// <summary>
/// Ghi bảng nối RBAC bằng HARD delete (repo generic chỉ soft-delete, sẽ đụng unique index
/// khi gán lại cùng cặp). Impl ở Infrastructure dùng <c>AppDbContext</c> trực tiếp — theo mẫu
/// <see cref="TourKit.Application.Reports.IReportQueries"/>. Mọi thao tác bám tenant hiện tại
/// (global query filter + interceptor gán TenantId).
/// </summary>
public interface IRbacStore
{
    /// <summary>Thay TOÀN BỘ quyền của một vai trò về đúng tập <paramref name="permissionIds"/>.</summary>
    Task ReplaceRolePermissionsAsync(Guid roleId, IReadOnlyCollection<Guid> permissionIds);

    /// <summary>Thay TOÀN BỘ vai trò của một user về đúng tập <paramref name="roleIds"/> (rỗng = bỏ hết).</summary>
    Task ReplaceUserRolesAsync(Guid userId, IReadOnlyCollection<Guid> roleIds);

    /// <summary>Xoá hẳn vai trò + toàn bộ RolePermission của nó (đã chặn nếu còn user ở tầng service).</summary>
    Task DeleteRoleCascadeAsync(Guid roleId);

    /// <summary>
    /// Id những user CÓ mã quyền này (qua vai trò), trong tenant hiện tại.
    ///
    /// Dùng để chỉ gợi ý @nhắc những người MỞ ĐƯỢC bản ghi. Nhắc người không có quyền xem thì họ
    /// nhận thông báo rồi bấm vào bị đá về màn đăng nhập — trông như hết phiên, không ai hiểu vì sao.
    /// </summary>
    Task<IReadOnlyList<Guid>> UserIdsWithPermissionAsync(string permissionCode);
}
