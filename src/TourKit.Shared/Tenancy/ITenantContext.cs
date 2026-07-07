namespace TourKit.Shared.Tenancy;

/// <summary>Tenant của request hiện tại. Được resolve từ JWT (Phase 0b) hoặc header (tạm thời).</summary>
public interface ITenantContext
{
    Guid TenantId { get; }
    bool HasTenant { get; }
}
