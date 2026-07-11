using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Common;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Sales.Dtos;
using TourKit.Tests.Support;

namespace TourKit.Tests.Sales;

/// <summary>
/// Chuyển báo giá chấp nhận → đơn (legacy DuyetBooking): đi qua flow đặt chỗ chuẩn,
/// doanh thu đơn = tổng báo giá, dòng dịch vụ đặt-ngoài sinh ServiceBooking, idempotent.
/// </summary>
public class QuoteConvertTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public QuoteConvertTests(AuthTestFactory factory) => _factory = factory;

    private async Task<HttpClient> LoggedInClientAsync(string slug)
    {
        var (s, email, password) = await _factory.SeedTenantUserAsync(slug);
        var client = _factory.CreateClient();
        var auth = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(s, email, password))).Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    private sealed record CustomerRow(Guid Id, string FullName);

    private static async Task<(Guid CustomerId, Guid DepartureId)> SeedCustomerAndDepartureAsync(HttpClient client, string suffix)
    {
        var template = await (await client.PostAsJsonAsync("/api/v1/tour-templates", new
        {
            Code = "TPL-CVT" + suffix, Title = "Đà Nẵng", TourType = (string?)null, TotalSlots = 30, ReservationHours = 24,
            PriceAdult = 5_000_000m, PriceChild = 3_000_000m, PriceChildSmall = 0m, PriceBaby = 0m,
            TermsNote = (string?)null,
        })).Content.ReadFromJsonAsync<TourTemplateDto>();

        var customer = await (await client.PostAsJsonAsync("/api/v1/customers",
            new { FullName = "Khách chuyển đơn", Phone = (string?)null }))
            .Content.ReadFromJsonAsync<CustomerRow>();

        var departure = await (await client.PostAsJsonAsync("/api/v1/tour-departures", new
        {
            TemplateId = template!.Id, Code = "DEP-CVT" + suffix, Title = "Đà Nẵng chuyến",
            DepartureDate = DateTimeOffset.UtcNow.AddDays(30), EndDate = (DateTimeOffset?)null, TotalSlots = 30,
        })).Content.ReadFromJsonAsync<DepartureDto>();

        return (customer!.Id, departure!.Id);
    }

    private static async Task<QuoteDto> CreateQuoteAsync(HttpClient client, Guid customerId, int status)
    {
        var response = await client.PostAsJsonAsync("/api/v1/quotes", new CreateQuoteDto(
            "BG-CVT-" + status, customerId, "Khách chuyển đơn", "Tour Đà Nẵng 3N2Đ",
            null, status, null,
            [
                // Phòng KS: theo khách, 3 đêm × vốn 500k, LN 20% → bán 600k.
                new CreateQuoteLineDto("Phòng khách sạn", 3, 0m, ServiceType: 1, Scope: 1, UnitCost: 500_000m, MarginPercent: 20m),
                // HDV: cả đoàn — KHÔNG sinh ServiceBooking (chi phí điều hành).
                new CreateQuoteLineDto("HDV", 3, 0m, ServiceType: 3, Scope: 0, UnitCost: 1_000_000m, MarginPercent: 10m),
            ],
            Adults: 2, Children: 1));
        return (await response.Content.ReadFromJsonAsync<QuoteDto>())!;
    }

    [Fact]
    public async Task Convert_accepted_quote_creates_order_with_quote_revenue_and_service_bookings()
    {
        var client = await LoggedInClientAsync("quote-cvt-a");
        var (customerId, departureId) = await SeedCustomerAndDepartureAsync(client, "A");
        var quote = await CreateQuoteAsync(client, customerId, status: 2);

        var response = await client.PostAsJsonAsync($"/api/v1/quotes/{quote.Id}/convert", new ConvertQuoteDto(departureId));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ConvertQuoteResultDto>();
        Assert.NotEqual(Guid.Empty, result!.OrderId);
        Assert.Equal(1, result.ServiceBookingCount);   // chỉ dòng KS (HDV là chi phí điều hành)

        // Báo giá ghi lại đơn đã sinh (idempotency + truy vết).
        var reloaded = await client.GetFromJsonAsync<QuoteDto>($"/api/v1/quotes/{quote.Id}");
        Assert.Equal(result.OrderId, reloaded!.ConvertedOrderId);

        // Chuyển lần 2 → 409 (đã chuyển).
        var again = await client.PostAsJsonAsync($"/api/v1/quotes/{quote.Id}/convert", new ConvertQuoteDto(departureId));
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Convert_fit_without_departure_creates_private_departure_and_order()
    {
        var client = await LoggedInClientAsync("quote-cvt-fit");
        var (customerId, _) = await SeedCustomerAndDepartureAsync(client, "F");
        var quote = await CreateQuoteAsync(client, customerId, status: 2);

        // FIT: không chọn chuyến — chỉ nhập ngày khởi hành → hệ tự tạo chuyến riêng.
        var response = await client.PostAsJsonAsync($"/api/v1/quotes/{quote.Id}/convert",
            new ConvertQuoteDto(null, DateTimeOffset.UtcNow.AddDays(20)));

        Assert.True(response.StatusCode == HttpStatusCode.OK,
            $"Expected 200, got {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        var result = await response.Content.ReadFromJsonAsync<ConvertQuoteResultDto>();
        Assert.NotEqual(Guid.Empty, result!.OrderId);

        // Chuyến riêng FIT-<code> đã được tạo, TotalSlots = đúng số khách (2 NL + 1 TE = 3 → chuyến kín).
        var departures = await client.GetFromJsonAsync<PagedResult<DepartureDto>>("/api/v1/tour-departures?page=1&size=200");
        var fit = departures!.Items.Single(d => d.Code == "FIT-" + quote.Code);
        Assert.Equal(3, fit.TotalSlots);
        Assert.Null(fit.TemplateId);
    }

    [Fact]
    public async Task Convert_fit_without_departure_and_date_returns_400()
    {
        var client = await LoggedInClientAsync("quote-cvt-nodate");
        var (customerId, _) = await SeedCustomerAndDepartureAsync(client, "N");
        var quote = await CreateQuoteAsync(client, customerId, status: 2);

        var response = await client.PostAsJsonAsync($"/api/v1/quotes/{quote.Id}/convert",
            new ConvertQuoteDto(null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Convert_non_accepted_quote_returns_400()
    {
        var client = await LoggedInClientAsync("quote-cvt-b");
        var (customerId, departureId) = await SeedCustomerAndDepartureAsync(client, "B");
        var draft = await CreateQuoteAsync(client, customerId, status: 0);

        var response = await client.PostAsJsonAsync($"/api/v1/quotes/{draft.Id}/convert", new ConvertQuoteDto(departureId));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
