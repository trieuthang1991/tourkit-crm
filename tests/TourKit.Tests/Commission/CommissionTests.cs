using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Commission.Dtos;
using TourKit.Application.Providers.Dtos;
using TourKit.Tests.Support;

using TourKit.Shared.Enums;

namespace TourKit.Tests.Commission;

public class CommissionTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public CommissionTests(AuthTestFactory factory) => _factory = factory;

    private async Task<HttpClient> LoggedInClientAsync(string slug)
    {
        var (s, email, password) = await _factory.SeedTenantUserAsync(slug);
        var client = _factory.CreateClient();
        var auth = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(s, email, password))).Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    // Dựng 1 Order doanh thu 13tr (2 người lớn 5tr + 1 trẻ em 3tr) qua chain đầy đủ, giống OrderCostTests.
    private static async Task<OrderDto> CreateOrderAsync(HttpClient client)
    {
        var template = await (await client.PostAsJsonAsync("/api/v1/tour-templates", new
        {
            Code = "TPL-COMM", Title = "Đà Nẵng", TourType = (string?)null, TotalSlots = 30, ReservationHours = 24,
            PriceAdult = 5_000_000m, PriceChild = 3_000_000m, PriceChildSmall = 0m, PriceBaby = 0m,
            TermsNote = (string?)null,
        })).Content.ReadFromJsonAsync<TourTemplateDto>();

        var customer = await (await client.PostAsJsonAsync("/api/v1/customers",
            new { FullName = "Nguyen Van Comm", Phone = (string?)null }))
            .Content.ReadFromJsonAsync<CustomerRow>();

        var departure = await (await client.PostAsJsonAsync("/api/v1/tour-departures", new
        {
            TemplateId = template!.Id, Code = "DEP-COMM", Title = "Đà Nẵng hoa hồng",
            DepartureDate = DateTimeOffset.UtcNow.AddDays(30), EndDate = (DateTimeOffset?)null, TotalSlots = 30,
        })).Content.ReadFromJsonAsync<DepartureDto>();

        var booking = await client.PostAsJsonAsync($"/api/v1/tour-departures/{departure!.Id}/bookings",
            new CreateBookingDto(customer!.Id, 2, 1, 0, 0));
        return (await booking.Content.ReadFromJsonAsync<OrderDto>())!;
    }

    private static async Task<ProviderDto> CreateProviderAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/providers", new CreateProviderDto(
            "NCC-COMM", "Khách sạn Hoa hồng", ProviderType.Hotel, null, null, null, null, null, null, null, 0, 1));
        return (await response.Content.ReadFromJsonAsync<ProviderDto>())!;
    }

    // Doanh thu 13tr, chi phí 3tr → lợi nhuận 10tr.
    private static async Task<OrderDto> CreateOrderWithProfitAsync(HttpClient client)
    {
        var order = await CreateOrderAsync(client);
        var provider = await CreateProviderAsync(client);
        await client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/costs",
            new CreateOrderCostDto(provider.Id, null, "Phòng khách sạn", 1, 3_000_000m, 3_000_000m, 0m, 0m, 0m, 1));
        return order;
    }

    [Fact]
    public async Task Get_profit_returns_revenue_cost_and_profit()
    {
        var client = await LoggedInClientAsync("comm-a");
        var order = await CreateOrderWithProfitAsync(client);

        var profit = await client.GetFromJsonAsync<OrderProfitDto>($"/api/v1/orders/{order.Id}/profit");
        Assert.Equal(13_000_000m, profit!.Revenue);
        Assert.Equal(3_000_000m, profit.Cost);
        Assert.Equal(10_000_000m, profit.Profit);
    }

    [Fact]
    public async Task Get_profit_with_unknown_order_is_404()
    {
        var client = await LoggedInClientAsync("comm-noorder");

        var response = await client.GetAsync($"/api/v1/orders/{Guid.NewGuid()}/profit");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Post_profit_share_computes_amount_and_is_listed()
    {
        var client = await LoggedInClientAsync("comm-b");
        var order = await CreateOrderWithProfitAsync(client);
        var userId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/profit-shares",
            new CreateProfitShareDto(userId, 10m));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var share = await response.Content.ReadFromJsonAsync<ProfitShareDto>();
        Assert.Equal(userId, share!.UserId);
        Assert.Equal(10m, share.Percentage);
        Assert.Equal(1_000_000m, share.Amount);
        Assert.Equal(10_000_000m, share.ProfitBase);

        var shares = await client.GetFromJsonAsync<List<ProfitShareDto>>(
            $"/api/v1/orders/{order.Id}/profit-shares");
        Assert.Single(shares!);
        Assert.Equal(share.Id, shares![0].Id);
    }

    [Fact]
    public async Task Post_profit_share_with_unknown_order_is_404()
    {
        var client = await LoggedInClientAsync("comm-share-noorder");

        var response = await client.PostAsJsonAsync($"/api/v1/orders/{Guid.NewGuid()}/profit-shares",
            new CreateProfitShareDto(Guid.NewGuid(), 10m));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    public async Task Post_profit_share_with_invalid_percentage_is_400(decimal percentage)
    {
        var client = await LoggedInClientAsync($"comm-badpct-{percentage}");
        var order = await CreateOrderWithProfitAsync(client);

        var response = await client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/profit-shares",
            new CreateProfitShareDto(Guid.NewGuid(), percentage));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Profit_shares_isolated_between_tenants()
    {
        var clientA = await LoggedInClientAsync("comm-iso-a");
        var orderA = await CreateOrderWithProfitAsync(clientA);
        await clientA.PostAsJsonAsync($"/api/v1/orders/{orderA.Id}/profit-shares",
            new CreateProfitShareDto(Guid.NewGuid(), 10m));

        var clientB = await LoggedInClientAsync("comm-iso-b");
        var sharesB = await clientB.GetFromJsonAsync<List<ProfitShareDto>>(
            $"/api/v1/orders/{orderA.Id}/profit-shares");
        Assert.Empty(sharesB!);

        var profitB = await clientB.GetAsync($"/api/v1/orders/{orderA.Id}/profit");
        Assert.Equal(HttpStatusCode.NotFound, profitB.StatusCode);
    }

    private sealed record CustomerRow(Guid Id, string FullName, string? Phone);
}
