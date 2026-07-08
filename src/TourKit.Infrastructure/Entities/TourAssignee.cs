using TourKit.Shared.Entities;

namespace TourKit.Infrastructure.Entities;

/// <summary>Người phụ trách tour — greenfield chuẩn hoá từ cột CSV hệ cũ (Tour.IdsNguoiTheoDoi/ManagerIds).</summary>
public sealed class TourAssignee : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid TourId { get; set; }
    public Guid UserId { get; set; }
    public AssigneeRole Role { get; set; }
}
