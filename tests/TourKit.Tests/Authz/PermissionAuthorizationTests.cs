using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TourKit.Api.Authz;
using TourKit.Infrastructure.Persistence;
using TourKit.Tests.Support;

namespace TourKit.Tests.Authz;

public class PermissionAuthorizationTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public PermissionAuthorizationTests(AuthTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Global_permissions_are_seeded()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var codes = await db.Permissions.Select(p => p.Code).ToListAsync();

        Assert.Contains(Permissions.TourCreate, codes);
        Assert.Equal(Permissions.All.Count, codes.Distinct().Count());
    }
}
