using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Application.Auth;
using TourKit.Application.Common;
using TourKit.Tests.Support;

namespace TourKit.Tests.Api;

/// <summary>Phân trang SQL fast-path (C2) + search không dấu / SĐT chuẩn hoá (S2).</summary>
public class CustomerSearchPagingTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;
    public CustomerSearchPagingTests(AuthTestFactory factory) => _factory = factory;

    private sealed record Row(Guid Id, string FullName, string? Phone);

    private async Task<HttpClient> LoggedInClientAsync(string slug)
    {
        var (s, email, password) = await _factory.SeedTenantUserAsync(slug);
        var client = _factory.CreateClient();
        var auth = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(s, email, password))).Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    [Fact]
    public async Task Paging_returns_correct_page_and_total()
    {
        var client = await LoggedInClientAsync("pg-a");
        for (var i = 1; i <= 25; i++)
        {
            (await client.PostAsJsonAsync("/api/v1/customers",
                new { FullName = $"Nguyễn A{i:00}", Phone = $"09000000{i:00}" })).EnsureSuccessStatusCode();
        }

        var page2 = await client.GetFromJsonAsync<PagedResult<Row>>("/api/v1/customers?page=2&size=20");

        Assert.NotNull(page2);
        Assert.Equal(25, page2!.Total);
        Assert.Equal(5, page2.Items.Count);
    }

    [Fact]
    public async Task Search_is_accent_insensitive()
    {
        var client = await LoggedInClientAsync("pg-b");
        (await client.PostAsJsonAsync("/api/v1/customers", new { FullName = "Nguyễn Văn Ân", Phone = "0911111111" })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/v1/customers", new { FullName = "Trần Thị Bình", Phone = "0922222222" })).EnsureSuccessStatusCode();

        var found = await client.GetFromJsonAsync<PagedResult<Row>>("/api/v1/customers?q=nguyen van an");

        Assert.NotNull(found);
        Assert.Equal("Nguyễn Văn Ân", Assert.Single(found!.Items).FullName);
    }

    [Fact]
    public async Task Search_by_phone_matches_normalized_prefix()
    {
        var client = await LoggedInClientAsync("pg-c");
        (await client.PostAsJsonAsync("/api/v1/customers", new { FullName = "Khách Quốc Tế", Phone = "+84901234567" })).EnsureSuccessStatusCode();

        var found = await client.GetFromJsonAsync<PagedResult<Row>>("/api/v1/customers?q=0901234567");

        Assert.NotNull(found);
        Assert.Equal("Khách Quốc Tế", Assert.Single(found!.Items).FullName);
    }
}
