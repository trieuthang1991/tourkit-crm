
namespace TourKit.Shared.Entities;

/// <summary>Subscription hiện tại của tenant trên một Plan. MVP: một subscription active/tenant.</summary>
public sealed class Subscription : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid PlanId { get; set; }
    public SubscriptionStatus Status { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
}
