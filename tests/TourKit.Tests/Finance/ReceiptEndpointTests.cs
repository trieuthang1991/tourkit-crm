using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Api.Booking;
using TourKit.Api.Catalog;
using TourKit.Api.Finance;
using TourKit.Tests.Support;

namespace TourKit.Tests.Finance;

public class ReceiptEndpointTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public ReceiptEndpointTests(AuthTestFactory factory) => _factory = factory;

    private async Task<HttpClient> LoggedInClientAsync(string slug)
    {
        var (s, email, password) = await _factory.SeedTenantUserAsync(slug);
        var client = _factory.CreateClient();
        var auth = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(s, email, password))).Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    // Dựng 1 đơn 13tr (2 người lớn 5tr + 1 trẻ em 3tr) và trả về orderId.
    private static async Task<Guid> CreateOrderAsync(HttpClient client)
    {
        var tpl = await (await client.PostAsJsonAsync("/api/v1/tour-templates", new
        {
            Code = "TPL", Title = "Tour", TourType = (string?)null, TotalSlots = 30, ReservationHours = 24,
            PriceAdult = 5_000_000m, PriceChild = 3_000_000m, PriceChildSmall = 0m, PriceBaby = 0m,
            TermsNote = (string?)null,
        })).Content.ReadFromJsonAsync<TourTemplateResponse>();

        var cus = await (await client.PostAsJsonAsync("/api/v1/customers",
            new { FullName = "A", Phone = (string?)null })).Content.ReadFromJsonAsync<CustomerRow>();

        var dep = await (await client.PostAsJsonAsync("/api/v1/tour-departures", new
        {
            TemplateId = tpl!.Id, Code = "DEP", Title = "Chuyến",
            DepartureDate = (DateTimeOffset?)null, EndDate = (DateTimeOffset?)null, TotalSlots = 30,
        })).Content.ReadFromJsonAsync<DepartureResponse>();

        var order = await (await client.PostAsJsonAsync($"/api/v1/tour-departures/{dep!.Id}/bookings",
            new CreateBookingRequest(cus!.Id, 2, 1, 0, 0))).Content.ReadFromJsonAsync<OrderResponse>();
        return order!.Id;
    }

    [Fact]
    public async Task Record_receipt_updates_balance()
    {
        var client = await LoggedInClientAsync("fin-a");
        var orderId = await CreateOrderAsync(client);

        // thu 5tr
        var rcp = await client.PostAsJsonAsync($"/api/v1/orders/{orderId}/receipts",
            new CreateReceiptRequest(5_000_000m, "cash", null, null));
        Assert.Equal(HttpStatusCode.Created, rcp.StatusCode);

        var balance = await client.GetFromJsonAsync<OrderBalanceResponse>($"/api/v1/orders/{orderId}/balance");
        Assert.Equal(13_000_000m, balance!.Total);
        Assert.Equal(5_000_000m, balance.Paid);
        Assert.Equal(8_000_000m, balance.Outstanding);

        // thu thêm 8tr → hết nợ
        await client.PostAsJsonAsync($"/api/v1/orders/{orderId}/receipts",
            new CreateReceiptRequest(8_000_000m, "transfer", null, "tất toán"));
        var balance2 = await client.GetFromJsonAsync<OrderBalanceResponse>($"/api/v1/orders/{orderId}/balance");
        Assert.Equal(13_000_000m, balance2!.Paid);
        Assert.Equal(0m, balance2.Outstanding);

        var receipts = await client.GetFromJsonAsync<List<ReceiptResponse>>($"/api/v1/orders/{orderId}/receipts");
        Assert.Equal(2, receipts!.Count);
    }

    [Fact]
    public async Task Non_positive_amount_is_400()
    {
        var client = await LoggedInClientAsync("fin-neg");
        var orderId = await CreateOrderAsync(client);

        var rcp = await client.PostAsJsonAsync($"/api/v1/orders/{orderId}/receipts",
            new CreateReceiptRequest(0m, "cash", null, null));
        Assert.Equal(HttpStatusCode.BadRequest, rcp.StatusCode);
    }

    [Fact]
    public async Task Receipts_isolated_between_tenants()
    {
        var clientA = await LoggedInClientAsync("fin-iso-a");
        var orderId = await CreateOrderAsync(clientA);
        await clientA.PostAsJsonAsync($"/api/v1/orders/{orderId}/receipts",
            new CreateReceiptRequest(1_000_000m, "cash", null, null));

        // tenant B không thấy phiếu của đơn A (đơn A không thuộc B → balance 404)
        var clientB = await LoggedInClientAsync("fin-iso-b");
        var res = await clientB.GetAsync($"/api/v1/orders/{orderId}/balance");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    private sealed record CustomerRow(Guid Id, string FullName, string? Phone);
}
