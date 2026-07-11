using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Reports.Dtos;
using TourKit.Application.Sales.Dtos;
using TourKit.Tests.Support;

namespace TourKit.Tests.Reports;

/// <summary>KPI phễu kinh doanh: báo giá → chấp nhận → chuyển đơn → thu tiền.</summary>
public class KpiReportTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public KpiReportTests(AuthTestFactory factory) => _factory = factory;

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

    [Fact]
    public async Task Kpi_reports_quote_funnel_and_collection()
    {
        var client = await LoggedInClientAsync("kpi-a");

        var customer = await (await client.PostAsJsonAsync("/api/v1/customers",
            new { FullName = "KH", Phone = (string?)null })).Content.ReadFromJsonAsync<CustomerRow>();

        // 3 báo giá: 1 nháp, 2 chấp nhận. 1 báo giá chấp nhận → chuyển đơn.
        await CreateQuoteAsync(client, customer!.Id, "BG-DRAFT", status: 0);
        var accepted1 = await CreateQuoteAsync(client, customer.Id, "BG-ACC1", status: 2);
        await CreateQuoteAsync(client, customer.Id, "BG-ACC2", status: 2);

        var (_, departureId) = await SeedDepartureAsync(client);
        var convert = await client.PostAsJsonAsync($"/api/v1/quotes/{accepted1.Id}/convert", new ConvertQuoteDto(departureId));
        convert.EnsureSuccessStatusCode();

        var kpi = await client.GetFromJsonAsync<KpiSummaryDto>("/api/v1/reports/kpi");

        Assert.Equal(3, kpi!.QuoteCount);
        Assert.Equal(2, kpi.QuoteAcceptedCount);
        Assert.Equal(1, kpi.QuoteConvertedCount);
        Assert.Equal(2m / 3m, kpi.AcceptanceRate, 3);   // 2/3 báo giá được chấp nhận
        Assert.Equal(0.5m, kpi.ConversionRate, 3);       // 1/2 báo giá chấp nhận thành đơn
        Assert.Equal(1, kpi.OrderCount);                 // đơn sinh từ chuyển báo giá
    }

    private static async Task<QuoteDto> CreateQuoteAsync(HttpClient client, Guid customerId, string code, int status)
    {
        var response = await client.PostAsJsonAsync("/api/v1/quotes", new CreateQuoteDto(
            code, customerId, "KH", "Tour", null, status, null,
            [new CreateQuoteLineDto("Trọn gói", 1, 5_000_000m)], Adults: 2));
        return (await response.Content.ReadFromJsonAsync<QuoteDto>())!;
    }

    private static async Task<(Guid TemplateId, Guid DepartureId)> SeedDepartureAsync(HttpClient client)
    {
        var tpl = await (await client.PostAsJsonAsync("/api/v1/tour-templates", new
        {
            Code = "TPL-KPI", Title = "Tour", TourType = (string?)null, TotalSlots = 30, ReservationHours = 24,
            PriceAdult = 5_000_000m, PriceChild = 3_000_000m, PriceChildSmall = 0m, PriceBaby = 0m, TermsNote = (string?)null,
        })).Content.ReadFromJsonAsync<TourTemplateDto>();
        var dep = await (await client.PostAsJsonAsync("/api/v1/tour-departures", new
        {
            TemplateId = tpl!.Id, Code = "DEP-KPI", Title = "Chuyến",
            DepartureDate = (DateTimeOffset?)null, EndDate = (DateTimeOffset?)null, TotalSlots = 30,
        })).Content.ReadFromJsonAsync<DepartureDto>();
        return (tpl.Id, dep!.Id);
    }
}
