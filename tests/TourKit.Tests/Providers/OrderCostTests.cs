using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Api.Booking;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Providers.Dtos;
using TourKit.Shared.Application;
using TourKit.Tests.Support;

using TourKit.Shared.Enums;

namespace TourKit.Tests.Providers;

public class OrderCostTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public OrderCostTests(AuthTestFactory factory) => _factory = factory;

    private async Task<HttpClient> LoggedInClientAsync(string slug)
    {
        var (s, email, password) = await _factory.SeedTenantUserAsync(slug);
        var client = _factory.CreateClient();
        var auth = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(s, email, password))).Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    // Dựng 1 Order qua chain đầy đủ: mẫu tour → khách → chuyến → đặt chỗ (giống BookingEndpointTests).
    private static async Task<OrderResponse> CreateOrderAsync(HttpClient client)
    {
        var template = await (await client.PostAsJsonAsync("/api/v1/tour-templates", new
        {
            Code = "TPL-COST", Title = "Đà Nẵng", TourType = (string?)null, TotalSlots = 30, ReservationHours = 24,
            PriceAdult = 5_000_000m, PriceChild = 3_000_000m, PriceChildSmall = 0m, PriceBaby = 0m,
            TermsNote = (string?)null,
        })).Content.ReadFromJsonAsync<TourTemplateDto>();

        var customer = await (await client.PostAsJsonAsync("/api/v1/customers",
            new { FullName = "Nguyen Van Cost", Phone = (string?)null }))
            .Content.ReadFromJsonAsync<CustomerRow>();

        var departure = await (await client.PostAsJsonAsync("/api/v1/tour-departures", new
        {
            TemplateId = template!.Id, Code = "DEP-COST", Title = "Đà Nẵng chi phí",
            DepartureDate = DateTimeOffset.UtcNow.AddDays(30), EndDate = (DateTimeOffset?)null, TotalSlots = 30,
        })).Content.ReadFromJsonAsync<DepartureResponse>();

        var booking = await client.PostAsJsonAsync($"/api/v1/tour-departures/{departure!.Id}/bookings",
            new CreateBookingRequest(customer!.Id, 1, 0, 0, 0));
        return (await booking.Content.ReadFromJsonAsync<OrderResponse>())!;
    }

    private static async Task<ProviderDto> CreateProviderAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/providers", new CreateProviderDto(
            "NCC-COST", "Khách sạn Chi phí", ProviderType.Hotel, null, null, null, null, null, null, null, 0, 1));
        return (await response.Content.ReadFromJsonAsync<ProviderDto>())!;
    }

    [Fact]
    public async Task Add_cost_is_recorded_listed_and_updates_order_total_cost()
    {
        var client = await LoggedInClientAsync("cost-a");
        var order = await CreateOrderAsync(client);
        var provider = await CreateProviderAsync(client);

        var costResponse = await client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/costs",
            new CreateOrderCostDto(provider.Id, "Phòng khách sạn", 1, 2_000_000m, 2_000_000m, 0m, 0m, 0m, 1));
        Assert.Equal(HttpStatusCode.Created, costResponse.StatusCode);
        var cost = await costResponse.Content.ReadFromJsonAsync<OrderCostDto>();
        Assert.Equal(2_000_000m, cost!.ActualAmount);
        Assert.Equal(provider.Id, cost.ProviderId);

        var costs = await client.GetFromJsonAsync<List<OrderCostDto>>($"/api/v1/orders/{order.Id}/costs");
        Assert.Single(costs!);
        Assert.Equal(cost.Id, costs![0].Id);

        // Order.TotalCost phải được recompute = tổng ActualAmount toàn bộ dòng chi phí của đơn.
        var orders = await client.GetFromJsonAsync<Paged<OrderResponse>>("/api/v1/orders");
        var updatedOrder = orders!.Items.Single(o => o.Id == order.Id);
        Assert.Equal(2_000_000m, updatedOrder.TotalCost);
        Assert.Equal(costs.Sum(c => c.ActualAmount), updatedOrder.TotalCost);

        // Thêm dòng chi phí thứ 2 → TotalCost cộng dồn.
        var costResponse2 = await client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/costs",
            new CreateOrderCostDto(provider.Id, "Phụ thu", 1, 500_000m, 500_000m, 0m, 0m, 0m, 1));
        Assert.Equal(HttpStatusCode.Created, costResponse2.StatusCode);

        var ordersAfterSecond = await client.GetFromJsonAsync<Paged<OrderResponse>>("/api/v1/orders");
        Assert.Equal(2_500_000m, ordersAfterSecond!.Items.Single(o => o.Id == order.Id).TotalCost);
    }

    [Fact]
    public async Task Add_cost_with_unknown_order_is_404()
    {
        var client = await LoggedInClientAsync("cost-noorder");
        var provider = await CreateProviderAsync(client);

        var response = await client.PostAsJsonAsync($"/api/v1/orders/{Guid.NewGuid()}/costs",
            new CreateOrderCostDto(provider.Id, "X", 1, 100_000m, 100_000m, 0m, 0m, 0m, 1));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Add_cost_with_unknown_provider_is_400()
    {
        var client = await LoggedInClientAsync("cost-noprov");
        var order = await CreateOrderAsync(client);

        var response = await client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/costs",
            new CreateOrderCostDto(Guid.NewGuid(), "X", 1, 100_000m, 100_000m, 0m, 0m, 0m, 1));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Add_cost_with_negative_actual_amount_is_400()
    {
        var client = await LoggedInClientAsync("cost-negative");
        var order = await CreateOrderAsync(client);
        var provider = await CreateProviderAsync(client);

        var response = await client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/costs",
            new CreateOrderCostDto(provider.Id, "X", 1, 100_000m, -1m, 0m, 0m, 0m, 1));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Costs_isolated_between_tenants()
    {
        var clientA = await LoggedInClientAsync("cost-iso-a");
        var orderA = await CreateOrderAsync(clientA);
        var providerA = await CreateProviderAsync(clientA);
        await clientA.PostAsJsonAsync($"/api/v1/orders/{orderA.Id}/costs",
            new CreateOrderCostDto(providerA.Id, "X", 1, 100_000m, 100_000m, 0m, 0m, 0m, 1));

        var clientB = await LoggedInClientAsync("cost-iso-b");
        var costsB = await clientB.GetFromJsonAsync<List<OrderCostDto>>($"/api/v1/orders/{orderA.Id}/costs");
        Assert.Empty(costsB!);
    }

    private sealed record CustomerRow(Guid Id, string FullName, string? Phone);
}
