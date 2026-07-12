using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Finance.Dtos;
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
        })).Content.ReadFromJsonAsync<TourTemplateDto>();

        var cus = await (await client.PostAsJsonAsync("/api/v1/customers",
            new { FullName = "A", Phone = (string?)null })).Content.ReadFromJsonAsync<CustomerRow>();

        var dep = await (await client.PostAsJsonAsync("/api/v1/tour-departures", new
        {
            TemplateId = tpl!.Id, Code = "DEP", Title = "Chuyến",
            DepartureDate = (DateTimeOffset?)null, EndDate = (DateTimeOffset?)null, TotalSlots = 30,
        })).Content.ReadFromJsonAsync<DepartureDto>();

        var order = await (await client.PostAsJsonAsync($"/api/v1/tour-departures/{dep!.Id}/bookings",
            new CreateBookingDto(cus!.Id, 2, 1, 0, 0))).Content.ReadFromJsonAsync<OrderDto>();
        return order!.Id;
    }

    [Fact]
    public async Task Only_approved_receipts_reduce_outstanding()
    {
        var client = await LoggedInClientAsync("fin-a");
        var orderId = await CreateOrderAsync(client);

        // thu 5tr — CHỜ DUYỆT → chưa tính vào công nợ
        var r1 = await (await client.PostAsJsonAsync($"/api/v1/orders/{orderId}/receipts",
            new CreateReceiptDto(5_000_000m, "cash", null, null))).Content.ReadFromJsonAsync<ReceiptDto>();
        Assert.False(r1!.IsRecognized);
        var pending = await client.GetFromJsonAsync<OrderBalanceDto>($"/api/v1/orders/{orderId}/balance");
        Assert.Equal(0m, pending!.Paid);

        // duyệt → tính 5tr, còn nợ 8tr
        (await client.PostAsync($"/api/v1/receipts/{r1.Id}/approve", null)).EnsureSuccessStatusCode();
        var afterApprove = await client.GetFromJsonAsync<OrderBalanceDto>($"/api/v1/orders/{orderId}/balance");
        Assert.Equal(5_000_000m, afterApprove!.Paid);
        Assert.Equal(8_000_000m, afterApprove.Outstanding);

        // thu 8tr + duyệt → hết nợ
        var r2 = await (await client.PostAsJsonAsync($"/api/v1/orders/{orderId}/receipts",
            new CreateReceiptDto(8_000_000m, "transfer", null, "tất toán"))).Content.ReadFromJsonAsync<ReceiptDto>();
        (await client.PostAsync($"/api/v1/receipts/{r2!.Id}/approve", null)).EnsureSuccessStatusCode();
        var settled = await client.GetFromJsonAsync<OrderBalanceDto>($"/api/v1/orders/{orderId}/balance");
        Assert.Equal(13_000_000m, settled!.Paid);
        Assert.Equal(0m, settled.Outstanding);
    }

    [Fact]
    public async Task Rejected_receipt_is_not_counted()
    {
        var client = await LoggedInClientAsync("fin-rej");
        var orderId = await CreateOrderAsync(client);

        var r = await (await client.PostAsJsonAsync($"/api/v1/orders/{orderId}/receipts",
            new CreateReceiptDto(5_000_000m, "cash", null, null))).Content.ReadFromJsonAsync<ReceiptDto>();
        (await client.PostAsync($"/api/v1/receipts/{r!.Id}/reject", null)).EnsureSuccessStatusCode();

        var bal = await client.GetFromJsonAsync<OrderBalanceDto>($"/api/v1/orders/{orderId}/balance");
        Assert.Equal(0m, bal!.Paid);
    }

    [Fact]
    public async Task Non_positive_amount_is_400()
    {
        var client = await LoggedInClientAsync("fin-neg");
        var orderId = await CreateOrderAsync(client);

        var rcp = await client.PostAsJsonAsync($"/api/v1/orders/{orderId}/receipts",
            new CreateReceiptDto(0m, "cash", null, null));
        Assert.Equal(HttpStatusCode.BadRequest, rcp.StatusCode);
    }

    [Fact]
    public async Task Receipts_isolated_between_tenants()
    {
        var clientA = await LoggedInClientAsync("fin-iso-a");
        var orderId = await CreateOrderAsync(clientA);
        await clientA.PostAsJsonAsync($"/api/v1/orders/{orderId}/receipts",
            new CreateReceiptDto(1_000_000m, "cash", null, null));

        // tenant B không thấy phiếu của đơn A (đơn A không thuộc B → balance 404)
        var clientB = await LoggedInClientAsync("fin-iso-b");
        var res = await clientB.GetAsync($"/api/v1/orders/{orderId}/balance");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task ListAll_receipts_returns_rows_with_order_code_and_customer()
    {
        var client = await LoggedInClientAsync("fin-rcp-all");
        var orderId = await CreateOrderAsync(client);
        await client.PostAsJsonAsync($"/api/v1/orders/{orderId}/receipts",
            new CreateReceiptDto(2_000_000m, "cash", null, null));

        var page = await client.GetFromJsonAsync<Paged<ReceiptListItemDto>>("/api/v1/receipts?page=1&size=20");
        var row = Assert.Single(page!.Items);
        Assert.Equal("A", row.CustomerName);
        Assert.False(string.IsNullOrEmpty(row.OrderCode));
        Assert.Equal(2_000_000m, row.Amount);
    }

    [Fact]
    public async Task ListAll_payments_returns_rows_with_order_code()
    {
        var client = await LoggedInClientAsync("fin-pay-all");
        var orderId = await CreateOrderAsync(client);
        await client.PostAsJsonAsync($"/api/v1/orders/{orderId}/payments",
            new CreatePaymentDto(null, null, 1_500_000m, "transfer", "NCC X", "Kế toán", null));

        var page = await client.GetFromJsonAsync<Paged<PaymentListItemDto>>("/api/v1/payments?page=1&size=20");
        var row = Assert.Single(page!.Items);
        Assert.False(string.IsNullOrEmpty(row.OrderCode));
        Assert.Equal(1_500_000m, row.Amount);
    }

    private sealed record Paged<T>(List<T> Items, int Total);

    private sealed record CustomerRow(Guid Id, string FullName, string? Phone);
}
