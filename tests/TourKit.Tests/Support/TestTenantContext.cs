using TourKit.Shared.Tenancy;

namespace TourKit.Tests.Support;

public sealed class TestTenantContext : ITenantContext
{
    public Guid TenantId { get; set; }
    public bool HasTenant => TenantId != Guid.Empty;
}
