namespace TourKit.Api.Tenancy;

public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantResolutionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, AmbientTenantContext tenant)
    {
        var claim = context.User.FindFirst("tenant_id")?.Value;
        if (Guid.TryParse(claim, out var tenantId))
        {
            tenant.SetTenant(tenantId);
        }

        await _next(context);
    }
}
