using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Application.Auth;
using TourKit.Tests.Support;

namespace TourKit.Tests.Api;

/// <summary>Thẻ thống kê Khách hàng — guard hành vi khi đổi GetStatsAsync sang đếm ở SQL (C1).</summary>
public class CustomerStatsEndpointTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;
    public CustomerStatsEndpointTests(AuthTestFactory factory) => _factory = factory;

    private sealed record Stats(int Total, int NewToday, int NewThisMonth, int FirstTimeBuyers, int RepeatBuyers);

    private async Task<HttpClient> LoggedInClientAsync(string slug)
    {
        var (s, email, password) = await _factory.SeedTenantUserAsync(slug);
        var client = _factory.CreateClient();
        var auth = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(email, password))).Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    [Fact]
    public async Task Stats_counts_customers_created_now()
    {
        var client = await LoggedInClientAsync("stats-a");
        (await client.PostAsJsonAsync("/api/v1/customers", new { FullName = "KH Một", Phone = "0900000001" })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/v1/customers", new { FullName = "KH Hai", Phone = "0900000002" })).EnsureSuccessStatusCode();

        var stats = await client.GetFromJsonAsync<Stats>("/api/v1/customers/stats");

        Assert.NotNull(stats);
        Assert.Equal(2, stats!.Total);
        Assert.Equal(2, stats.NewToday);
        Assert.Equal(2, stats.NewThisMonth);
        Assert.Equal(0, stats.FirstTimeBuyers);   // chưa có đơn
        Assert.Equal(0, stats.RepeatBuyers);
    }
}
