using TourKit.Shared.Tenancy;

namespace TourKit.Api.Tenancy;

/// <summary>
/// Tạm thời resolve tenant từ header "X-Tenant-Id".
/// Phase 0b sẽ thay bằng claim trong JWT (không tin client).
/// </summary>
public sealed class HttpTenantContext : ITenantContext
{
    public Guid TenantId { get; }
    public bool HasTenant => TenantId != Guid.Empty;

    public HttpTenantContext(IHttpContextAccessor accessor)
    {
        var raw = accessor.HttpContext?.Request.Headers["X-Tenant-Id"].ToString();
        TenantId = Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }
}
