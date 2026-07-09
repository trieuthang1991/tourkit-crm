
namespace TourKit.Shared.Entities;

/// <summary>Chăm sóc khách hàng (legacy Customer_Care): lịch/nội dung chăm sóc + nhắc hẹn + phản hồi.</summary>
public sealed class CustomerCare : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid CustomerId { get; set; }
    public string Title { get; set; } = string.Empty;   // Care_Title
    public string? Detail { get; set; }                   // Care_Detail
    public DateTimeOffset? RemindAt { get; set; }         // TimeCareRemind
    public string? Feedback { get; set; }                 // Feedback
    public Guid? AssignedToUserId { get; set; }
    public int Status { get; set; }
}
