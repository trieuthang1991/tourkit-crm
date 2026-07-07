using TourKit.Shared.Tenancy;

namespace TourKit.Api.Tenancy;

/// <summary>
/// Tenant của request hiện tại — nguồn có thể là claim JWT (qua TenantResolutionMiddleware)
/// hoặc set tường minh khi login/seed/provisioning (chưa có claim). Scoped: mỗi request 1 instance.
/// </summary>
public sealed class AmbientTenantContext : ITenantContext
{
    public Guid TenantId { get; private set; }
    public bool HasTenant => TenantId != Guid.Empty;

    public void SetTenant(Guid tenantId) => TenantId = tenantId;
}
