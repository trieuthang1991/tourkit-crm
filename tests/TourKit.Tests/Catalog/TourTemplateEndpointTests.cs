using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Api.Catalog;
using TourKit.Tests.Support;

namespace TourKit.Tests.Catalog;

public class TourTemplateEndpointTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public TourTemplateEndpointTests(AuthTestFactory factory) => _factory = factory;

    private async Task<HttpClient> LoggedInClientAsync(string slug)
    {
        var (s, email, password) = await _factory.SeedTenantUserAsync(slug);
        var client = _factory.CreateClient();
        var auth = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(s, email, password))).Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    private static CreateTourTemplateRequest Sample(string code) =>
        new(code, "Đà Nẵng 3N2Đ", "domestic", 30, 24, 5_000_000m, 3_000_000m, 2_000_000m, 0m, "Điều khoản");

    [Fact]
    public async Task Create_then_list_and_get()
    {
        var client = await LoggedInClientAsync("cat-a");

        var created = await client.PostAsJsonAsync("/api/v1/tour-templates", Sample("T-001"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var dto = await created.Content.ReadFromJsonAsync<TourTemplateResponse>();
        Assert.NotNull(dto);
        Assert.Equal(5_000_000m, dto!.PriceAdult);

        var list = await client.GetFromJsonAsync<List<TourTemplateResponse>>("/api/v1/tour-templates");
        Assert.Single(list!);

        var got = await client.GetFromJsonAsync<TourTemplateResponse>($"/api/v1/tour-templates/{dto.Id}");
        Assert.Equal("T-001", got!.Code);
    }

    [Fact]
    public async Task Requires_auth()
    {
        var client = _factory.CreateClient();   // không login
        var res = await client.GetAsync("/api/v1/tour-templates");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Isolated_between_tenants()
    {
        var clientA = await LoggedInClientAsync("cat-iso-a");
        await clientA.PostAsJsonAsync("/api/v1/tour-templates", Sample("A-1"));

        var clientB = await LoggedInClientAsync("cat-iso-b");
        var listB = await clientB.GetFromJsonAsync<List<TourTemplateResponse>>("/api/v1/tour-templates");
        Assert.Empty(listB!);
    }
}
