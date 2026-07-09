namespace TourKit.Shared.Entities;

/// <summary>Vai trò người phụ trách tour — greenfield chuẩn hoá từ cột CSV hệ cũ (Tour.IdsNguoiTheoDoi/ManagerIds).</summary>
public enum AssigneeRole
{
    Manager = 1,
    Watcher = 2,
    Assignee = 3,
}
