using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Application.Common;
using TourKit.Tests.Support;

namespace TourKit.Tests.Api;

public class CustomerEndpointIsolationTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public CustomerEndpointIsolationTests(AuthTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Each_tenant_sees_only_its_own_customers_over_http()
    {
        var a = await _factory.SeedTenantUserAsync("acme-iso");
        var b = await _factory.SeedTenantUserAsync("beta-iso");
        var client = _factory.CreateClient();

        var tokenA = (await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(a.slug, a.email, a.password))).Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenA);
        (await client.PostAsJsonAsync("/api/v1/customers",
            new { FullName = "A-http", Phone = (string?)null })).EnsureSuccessStatusCode();

        var tokenB = (await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(b.slug, b.email, b.password))).Content.ReadFromJsonAsync<AuthResponse>())!.AccessToken;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenB);
        var listB = await client.GetFromJsonAsync<PagedResult<CustomerDto>>("/api/v1/customers");

        Assert.NotNull(listB);
        Assert.Empty(listB!.Items);
    }

    private sealed record CustomerDto(Guid Id, string FullName, string? Phone);
}
