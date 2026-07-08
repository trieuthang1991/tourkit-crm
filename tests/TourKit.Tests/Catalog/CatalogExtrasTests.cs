using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Api.Catalog;
using TourKit.Api.Catalog.Features;
using TourKit.Infrastructure.Entities;
using TourKit.Tests.Support;

namespace TourKit.Tests.Catalog;

public class CatalogExtrasTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public CatalogExtrasTests(AuthTestFactory factory) => _factory = factory;

    private async Task<HttpClient> LoggedInClientAsync(string slug)
    {
        var (s, email, password) = await _factory.SeedTenantUserAsync(slug);
        var client = _factory.CreateClient();
        var auth = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(s, email, password))).Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    private static CreateTourTemplateRequest SampleTemplate(string code) =>
        new(code, "Đà Nẵng 3N2Đ", "domestic", 30, 24, 5_000_000m, 3_000_000m, 2_000_000m, 0m, "Điều khoản");

    [Fact]
    public async Task Create_market_type_then_list_with_parent_child()
    {
        var client = await LoggedInClientAsync("mkt-a");

        var parent = await client.PostAsJsonAsync("/api/v1/market-types",
            new CreateMarketTypeCommand("Nội địa", null, 1));
        Assert.Equal(HttpStatusCode.Created, parent.StatusCode);
        var parentDto = await parent.Content.ReadFromJsonAsync<MarketTypeDto>();

        var child = await client.PostAsJsonAsync("/api/v1/market-types",
            new CreateMarketTypeCommand("Miền Trung", parentDto!.Id, 1));
        Assert.Equal(HttpStatusCode.Created, child.StatusCode);

        var list = await client.GetFromJsonAsync<List<MarketTypeDto>>("/api/v1/market-types");
        Assert.Equal(2, list!.Count);
        Assert.Contains(list, m => m.Name == "Nội địa" && m.ParentId == null);
        Assert.Contains(list, m => m.Name == "Miền Trung" && m.ParentId == parentDto.Id);
    }

    [Fact]
    public async Task Market_types_are_isolated_between_tenants()
    {
        var clientA = await LoggedInClientAsync("mkt-iso-a");
        await clientA.PostAsJsonAsync("/api/v1/market-types", new CreateMarketTypeCommand("A", null, 1));

        var clientB = await LoggedInClientAsync("mkt-iso-b");
        var listB = await clientB.GetFromJsonAsync<List<MarketTypeDto>>("/api/v1/market-types");
        Assert.Empty(listB!);
    }

    [Fact]
    public async Task Put_price_scenarios_replaces_all_and_orders_by_from_qty()
    {
        var client = await LoggedInClientAsync("price-a");
        var template = await (await client.PostAsJsonAsync("/api/v1/tour-templates", SampleTemplate("P-001")))
            .Content.ReadFromJsonAsync<TourTemplateResponse>();

        var first = new[]
        {
            new PriceScenarioRequest(10, 20, 4_500_000m),
            new PriceScenarioRequest(1, 9, 5_000_000m),
        };
        var put1 = await client.PutAsJsonAsync($"/api/v1/tour-templates/{template!.Id}/price-scenarios", first);
        Assert.Equal(HttpStatusCode.NoContent, put1.StatusCode);

        var got1 = await client.GetFromJsonAsync<List<PriceScenarioResponse>>(
            $"/api/v1/tour-templates/{template.Id}/price-scenarios");
        Assert.Equal(2, got1!.Count);
        Assert.Equal(1, got1[0].FromQty);
        Assert.Equal(10, got1[1].FromQty);

        var second = new[] { new PriceScenarioRequest(1, 30, 4_000_000m) };
        var put2 = await client.PutAsJsonAsync($"/api/v1/tour-templates/{template.Id}/price-scenarios", second);
        Assert.Equal(HttpStatusCode.NoContent, put2.StatusCode);

        var got2 = await client.GetFromJsonAsync<List<PriceScenarioResponse>>(
            $"/api/v1/tour-templates/{template.Id}/price-scenarios");
        Assert.Single(got2!);
        Assert.Equal(4_000_000m, got2![0].UnitPrice);
    }

    [Fact]
    public async Task Put_assignees_on_tour_template_replaces_all()
    {
        var client = await LoggedInClientAsync("assignee-a");
        var template = await (await client.PostAsJsonAsync("/api/v1/tour-templates", SampleTemplate("AS-001")))
            .Content.ReadFromJsonAsync<TourTemplateResponse>();

        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();
        var body = new[]
        {
            new AssigneeRequest(userA, AssigneeRole.Manager),
            new AssigneeRequest(userB, AssigneeRole.Watcher),
        };

        // TourTemplate LÀ Tour (TPT) — Id của template dùng thẳng làm tourId.
        var put = await client.PutAsJsonAsync($"/api/v1/tours/{template!.Id}/assignees", body);
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var got = await client.GetFromJsonAsync<List<AssigneeResponse>>($"/api/v1/tours/{template.Id}/assignees");
        Assert.Equal(2, got!.Count);
        Assert.Contains(got, a => a.UserId == userA && a.Role == AssigneeRole.Manager);
        Assert.Contains(got, a => a.UserId == userB && a.Role == AssigneeRole.Watcher);
    }
}
