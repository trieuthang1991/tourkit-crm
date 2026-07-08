using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Api.Provisioning;
using TourKit.Tests.Support;

namespace TourKit.Tests.Provisioning;

public class RegistrationEndpointTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public RegistrationEndpointTests(AuthTestFactory factory) => _factory = factory;

    private static RegisterTenantRequest Sample(string slug) =>
        new("Công ty " + slug, slug, $"admin@{slug}.com", "P@ssw0rd!", "Admin");

    [Fact]
    public async Task Register_then_admin_can_login_and_has_full_permissions()
    {
        var client = _factory.CreateClient();

        var reg = await client.PostAsJsonAsync("/api/v1/registration", Sample("newco"));
        Assert.Equal(HttpStatusCode.Created, reg.StatusCode);
        var created = await reg.Content.ReadFromJsonAsync<RegistrationResponse>();
        Assert.NotNull(created);

        // login bằng admin vừa tạo
        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest("newco", "admin@newco.com", "P@ssw0rd!"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();

        // admin có quyền tạo tour-template (tour.create)
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        var create = await client.PostAsJsonAsync("/api/v1/tour-templates", new
        {
            Code = "T-1", Title = "Tour", TourType = (string?)null, TotalSlots = 10, ReservationHours = 24,
            PriceAdult = 1m, PriceChild = 1m, PriceChildSmall = 1m, PriceBaby = 0m, TermsNote = (string?)null,
        });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
    }

    [Fact]
    public async Task Duplicate_slug_returns_409()
    {
        var client = _factory.CreateClient();
        (await client.PostAsJsonAsync("/api/v1/registration", Sample("dupco"))).EnsureSuccessStatusCode();

        var again = await client.PostAsJsonAsync("/api/v1/registration", Sample("dupco"));
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Short_password_returns_400()
    {
        var client = _factory.CreateClient();
        var res = await client.PostAsJsonAsync("/api/v1/registration",
            new RegisterTenantRequest("C", "shortpw", "a@b.com", "123", "A"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
}
