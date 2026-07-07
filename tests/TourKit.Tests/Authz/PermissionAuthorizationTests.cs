using System.Net.Http.Json;
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

    private async Task<HttpClient> LoginAsync((string slug, string email, string password) seed)
    {
        var client = _factory.CreateClient();
        var auth = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new TourKit.Api.Auth.LoginRequest(seed.slug, seed.email, seed.password)))
            .Content.ReadFromJsonAsync<TourKit.Api.Auth.AuthResponse>();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    [Fact]
    public async Task User_without_permission_gets_403()
    {
        // chỉ có quyền xem tour, KHÔNG có quyền tạo
        var seed = await _factory.SeedTenantUserWithPermissionsAsync("perm-a", Permissions.TourView);
        var client = await LoginAsync(seed);

        var view = await client.GetAsync("/api/v1/tour-templates");
        Assert.Equal(System.Net.HttpStatusCode.OK, view.StatusCode);

        var create = await client.PostAsJsonAsync("/api/v1/tour-templates", new
        {
            Code = "X-1", Title = "X", TourType = (string?)null, TotalSlots = 10, ReservationHours = 24,
            PriceAdult = 1m, PriceChild = 1m, PriceChildSmall = 1m, PriceBaby = 0m, TermsNote = (string?)null,
        });
        Assert.Equal(System.Net.HttpStatusCode.Forbidden, create.StatusCode);
    }
}
