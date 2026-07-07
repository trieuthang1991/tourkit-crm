using TourKit.Api.Tenancy;

namespace TourKit.Tests.Tenancy;

public class AmbientTenantContextTests
{
    [Fact]
    public void Starts_with_no_tenant_then_can_be_set()
    {
        var ctx = new AmbientTenantContext();
        Assert.False(ctx.HasTenant);

        var id = Guid.NewGuid();
        ctx.SetTenant(id);

        Assert.True(ctx.HasTenant);
        Assert.Equal(id, ctx.TenantId);
    }
}
