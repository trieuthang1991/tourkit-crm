using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Finance.Dtos;
using TourKit.Application.Reports.Dtos;
using TourKit.Tests.Support;

namespace TourKit.Tests.Reports;

public class DebtReportTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public DebtReportTests(AuthTestFactory factory) => _factory = factory;

    private async Task<HttpClient> LoggedInClientAsync(string slug)
    {
        var (s, email, password) = await _factory.SeedTenantUserAsync(slug);
        var client = _factory.CreateClient();
        var auth = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(s, email, password))).Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    // Đơn 13tr (2 NL 5tr + 1 TE 3tr), trả về orderId.
    private static async Task<Guid> CreateOrderAsync(HttpClient client)
    {
        var tpl = await (await client.PostAsJsonAsync("/api/v1/tour-templates", new
        {
            Code = "TPL-" + Guid.NewGuid().ToString("N")[..6], Title = "Tour", TourType = (string?)null,
            TotalSlots = 30, ReservationHours = 24,
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

    private static async Task ApproveReceiptAsync(HttpClient client, Guid orderId, decimal amount)
    {
        var r = await (await client.PostAsJsonAsync($"/api/v1/orders/{orderId}/receipts",
            new CreateReceiptDto(amount, "cash", null, null))).Content.ReadFromJsonAsync<ReceiptDto>();
        (await client.PostAsync($"/api/v1/receipts/{r!.Id}/approve", null)).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Debt_report_lists_orders_still_owing()
    {
        var client = await LoggedInClientAsync("debt-a");

        var owing = await CreateOrderAsync(client);      // 13tr
        await ApproveReceiptAsync(client, owing, 5_000_000m);   // còn nợ 8tr

        var settled = await CreateOrderAsync(client);    // 13tr
        await ApproveReceiptAsync(client, settled, 13_000_000m); // hết nợ

        var rows = await client.GetFromJsonAsync<List<OrderDebtRowDto>>("/api/v1/reports/order-debt");

        Assert.Single(rows!);                            // chỉ đơn còn nợ
        Assert.Equal(owing, rows![0].OrderId);
        Assert.Equal(13_000_000m, rows[0].Total);
        Assert.Equal(5_000_000m, rows[0].Paid);
        Assert.Equal(8_000_000m, rows[0].Outstanding);
    }

    [Fact]
    public async Task Debt_report_isolated_between_tenants()
    {
        var clientA = await LoggedInClientAsync("debt-iso-a");
        var owing = await CreateOrderAsync(clientA);
        await ApproveReceiptAsync(clientA, owing, 1_000_000m);

        var clientB = await LoggedInClientAsync("debt-iso-b");
        var rowsB = await clientB.GetFromJsonAsync<List<OrderDebtRowDto>>("/api/v1/reports/order-debt");
        Assert.Empty(rowsB!);
    }

    private sealed record CustomerRow(Guid Id, string FullName, string? Phone);
}
