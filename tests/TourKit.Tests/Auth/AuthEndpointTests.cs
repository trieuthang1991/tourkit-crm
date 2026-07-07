using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Tests.Support;

namespace TourKit.Tests.Auth;

public class AuthEndpointTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public AuthEndpointTests(AuthTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Login_valid_returns_token_then_can_call_protected_endpoint()
    {
        var (slug, email, password) = await _factory.SeedTenantUserAsync("acme");
        var client = _factory.CreateClient();

        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(slug, email, password));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);

        // Không token → 401
        var anon = await client.GetAsync("/api/v1/customers");
        Assert.Equal(HttpStatusCode.Unauthorized, anon.StatusCode);

        // Có token → 200
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        var ok = await client.GetAsync("/api/v1/customers");
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
    }

    [Fact]
    public async Task Login_wrong_password_returns_401()
    {
        var (slug, email, _) = await _factory.SeedTenantUserAsync("beta");
        var client = _factory.CreateClient();

        var res = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(slug, email, "wrong-password"));
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Two_tenants_are_isolated_via_jwt()
    {
        var a = await _factory.SeedTenantUserAsync("gamma");
        var b = await _factory.SeedTenantUserAsync("delta");
        var client = _factory.CreateClient();

        // user A tạo khách
        var loginA = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(a.slug, a.email, a.password))).Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginA!.AccessToken);
        await client.PostAsJsonAsync("/api/v1/customers", new { FullName = "Khach cua A", Phone = (string?)null });

        // user B đọc — không thấy khách của A
        var loginB = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(b.slug, b.email, b.password))).Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginB!.AccessToken);
        var listB = await client.GetFromJsonAsync<List<CustomerRow>>("/api/v1/customers");

        Assert.NotNull(listB);
        Assert.Empty(listB!);

        // refresh token của A vẫn cấp token mới
        var refreshed = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(loginA.RefreshToken));
        Assert.Equal(HttpStatusCode.OK, refreshed.StatusCode);
    }

    private sealed record CustomerRow(Guid Id, string FullName, string? Phone);
}
