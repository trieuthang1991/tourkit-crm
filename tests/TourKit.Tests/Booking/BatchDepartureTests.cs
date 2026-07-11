using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Common;
using TourKit.Tests.Support;

namespace TourKit.Tests.Booking;

/// <summary>Mở hàng loạt chuyến từ mẫu (legacy BatchCreateTour): mỗi ngày → 1 chuyến Code=Prefix-STT.</summary>
public class BatchDepartureTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public BatchDepartureTests(AuthTestFactory factory) => _factory = factory;

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
    public async Task Batch_creates_one_departure_per_date_inheriting_template()
    {
        var client = await LoggedInClientAsync("batch-a");
        var tpl = await (await client.PostAsJsonAsync("/api/v1/tour-templates", new
        {
            Code = "TPL-BATCH", Title = "Tour tuần", TourType = "domestic", TotalSlots = 25, ReservationHours = 24,
            PriceAdult = 5_000_000m, PriceChild = 3_000_000m, PriceChildSmall = 0m, PriceBaby = 0m, TermsNote = (string?)null,
        })).Content.ReadFromJsonAsync<TourTemplateDto>();

        var start = DateTimeOffset.UtcNow.AddDays(7);
        var items = new[]
        {
            new BatchDepartureItemDto(start, start.AddDays(2)),
            new BatchDepartureItemDto(start.AddDays(7), start.AddDays(9)),
            new BatchDepartureItemDto(start.AddDays(14), start.AddDays(16)),
        };

        var response = await client.PostAsJsonAsync("/api/v1/tour-departures/batch",
            new BatchCreateDeparturesDto(tpl!.Id, "TUAN", null, 0, items));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<BatchCreateResultDto>();
        Assert.Equal(3, result!.Created);
        Assert.Equal(new[] { "TUAN-1", "TUAN-2", "TUAN-3" }, result.Departures.Select(d => d.Code).ToArray());
        Assert.All(result.Departures, d => Assert.Equal(25, d.TotalSlots));   // kế thừa sức chứa mẫu (TotalSlots=0)
        Assert.All(result.Departures, d => Assert.Equal("Tour tuần", d.Title)); // kế thừa tiêu đề mẫu

        var list = await client.GetFromJsonAsync<PagedResult<DepartureDto>>("/api/v1/tour-departures?page=1&size=200");
        Assert.Equal(3, list!.Items.Count);
    }

    [Fact]
    public async Task Batch_empty_items_returns_400()
    {
        var client = await LoggedInClientAsync("batch-empty");
        var tpl = await (await client.PostAsJsonAsync("/api/v1/tour-templates", new
        {
            Code = "TPL-E", Title = "T", TourType = (string?)null, TotalSlots = 10, ReservationHours = 24,
            PriceAdult = 1m, PriceChild = 1m, PriceChildSmall = 0m, PriceBaby = 0m, TermsNote = (string?)null,
        })).Content.ReadFromJsonAsync<TourTemplateDto>();

        var response = await client.PostAsJsonAsync("/api/v1/tour-departures/batch",
            new BatchCreateDeparturesDto(tpl!.Id, "X", null, 0, []));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
