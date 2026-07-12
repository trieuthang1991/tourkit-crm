using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Application.Common;
using TourKit.Tests.Support;

namespace TourKit.Tests.Api;

/// <summary>Thanh lọc màn Data khách hàng: binding [FromQuery] CustomerListFilter + endpoint facets.</summary>
public class CustomerFilterEndpointTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public CustomerFilterEndpointTests(AuthTestFactory factory) => _factory = factory;

    private async Task<HttpClient> LoggedInClientAsync(string slug)
    {
        var (s, email, password) = await _factory.SeedTenantUserAsync(slug);
        var client = _factory.CreateClient();
        var auth = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(s, email, password))).Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    private sealed record Row(Guid Id, string FullName, string? City);

    private sealed record FilterOptions(
        List<string> Sources, List<string> Cities, List<string> MarketGroups, List<string> Campaigns,
        List<string> Collaborators, List<string> Branches, List<string> Groups, List<string> Departments,
        List<string> Tags, List<string> Segments);

    private static async Task SeedAsync(HttpClient client)
    {
        (await client.PostAsJsonAsync("/api/v1/customers", new
        {
            FullName = "Khách Hà Nội", City = "Hà Nội", Source = "Facebook",
            Tags = new[] { "VIP" }, Campaign = "Hè 2026",
        })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/v1/customers", new
        {
            FullName = "Khách Sài Gòn", City = "TP.HCM", Source = "Zalo",
            Tags = new[] { "B2B" }, Campaign = "Thu 2026",
        })).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task List_filters_by_city_query_param()
    {
        var client = await LoggedInClientAsync("filt-a");
        await SeedAsync(client);

        var list = await client.GetFromJsonAsync<PagedResult<Row>>(
            "/api/v1/customers?city=" + Uri.EscapeDataString("Hà Nội"));

        Assert.Equal("Khách Hà Nội", Assert.Single(list!.Items).FullName);
    }

    [Fact]
    public async Task List_filters_by_tag_query_param()
    {
        var client = await LoggedInClientAsync("filt-b");
        await SeedAsync(client);

        var list = await client.GetFromJsonAsync<PagedResult<Row>>("/api/v1/customers?tag=VIP");

        Assert.Equal("Khách Hà Nội", Assert.Single(list!.Items).FullName);
    }

    [Fact]
    public async Task FilterOptions_returns_distinct_facets()
    {
        var client = await LoggedInClientAsync("filt-c");
        await SeedAsync(client);

        var opts = await client.GetFromJsonAsync<FilterOptions>("/api/v1/customers/filter-options");

        Assert.Contains("Facebook", opts!.Sources);
        Assert.Contains("Zalo", opts.Sources);
        Assert.Contains("Hà Nội", opts.Cities);
        Assert.Contains("VIP", opts.Tags);
        Assert.Contains("B2B", opts.Tags);
    }
}
