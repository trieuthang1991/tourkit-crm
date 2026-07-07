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

    [Fact]
    public async Task Update_then_soft_delete()
    {
        var client = await LoggedInClientAsync("cat-upd");
        var dto = await (await client.PostAsJsonAsync("/api/v1/tour-templates", Sample("U-1")))
            .Content.ReadFromJsonAsync<TourTemplateResponse>();

        var upd = await client.PutAsJsonAsync($"/api/v1/tour-templates/{dto!.Id}",
            new UpdateTourTemplateRequest("Huế 2N1Đ", "domestic", 20, 12, 4_000_000m, 2_500_000m, 1_500_000m, 0m, "ĐK mới"));
        Assert.Equal(HttpStatusCode.NoContent, upd.StatusCode);

        var got = await client.GetFromJsonAsync<TourTemplateResponse>($"/api/v1/tour-templates/{dto.Id}");
        Assert.Equal("Huế 2N1Đ", got!.Title);

        var del = await client.DeleteAsync($"/api/v1/tour-templates/{dto.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var list = await client.GetFromJsonAsync<List<TourTemplateResponse>>("/api/v1/tour-templates");
        Assert.Empty(list!);   // soft-deleted bị filter ẩn
    }

    [Fact]
    public async Task Set_and_get_itinerary()
    {
        var client = await LoggedInClientAsync("cat-itin");
        var dto = await (await client.PostAsJsonAsync("/api/v1/tour-templates", Sample("I-1")))
            .Content.ReadFromJsonAsync<TourTemplateResponse>();

        var days = new[]
        {
            new ItineraryDayRequest(1, "Ngày 1: Khởi hành", "Bay Hà Nội - Đà Nẵng"),
            new ItineraryDayRequest(2, "Ngày 2: Tham quan", "Bà Nà Hills"),
        };
        var put = await client.PutAsJsonAsync($"/api/v1/tour-templates/{dto!.Id}/itinerary", days);
        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);

        var got = await client.GetFromJsonAsync<List<ItineraryDayResponse>>($"/api/v1/tour-templates/{dto.Id}/itinerary");
        Assert.Equal(2, got!.Count);
        Assert.Equal("Ngày 1: Khởi hành", got[0].Title);
    }
}
