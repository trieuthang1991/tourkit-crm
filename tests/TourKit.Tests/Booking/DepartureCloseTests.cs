using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Api.Booking;
using TourKit.Api.Catalog;
using TourKit.Tests.Support;

namespace TourKit.Tests.Booking;

public sealed class DepartureCloseTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public DepartureCloseTests(AuthTestFactory factory) => _factory = factory;

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
    public async Task Close_then_second_close_conflicts_and_booking_blocked()
    {
        var client = await LoggedInClientAsync("dep-close");

        var template = await (await client.PostAsJsonAsync("/api/v1/tour-templates", new
        {
            Code = "TPL-C", Title = "Đà Nẵng", TourType = (string?)null, TotalSlots = 30, ReservationHours = 24,
            PriceAdult = 5_000_000m, PriceChild = 3_000_000m, PriceChildSmall = 0m, PriceBaby = 0m,
            TermsNote = (string?)null,
        })).Content.ReadFromJsonAsync<TourTemplateResponse>();

        var customer = await (await client.PostAsJsonAsync("/api/v1/customers",
            new { FullName = "Nguyen Van A", Phone = (string?)null }))
            .Content.ReadFromJsonAsync<CustomerRow>();

        var dep = await (await client.PostAsJsonAsync("/api/v1/tour-departures", new
        {
            TemplateId = template!.Id, Code = "DEP-C", Title = "Đà Nẵng 20/07",
            DepartureDate = DateTimeOffset.UtcNow.AddDays(30), EndDate = (DateTimeOffset?)null, TotalSlots = 30,
        })).Content.ReadFromJsonAsync<DepartureResponse>();

        var close1 = await client.PostAsync($"/api/v1/tour-departures/{dep!.Id}/close", null);
        Assert.Equal(HttpStatusCode.OK, close1.StatusCode);

        var close2 = await client.PostAsync($"/api/v1/tour-departures/{dep.Id}/close", null);
        Assert.Equal(HttpStatusCode.Conflict, close2.StatusCode);

        var book = await client.PostAsJsonAsync($"/api/v1/tour-departures/{dep.Id}/bookings",
            new CreateBookingRequest(customer!.Id, 1, 0, 0, 0));
        Assert.Equal(HttpStatusCode.Conflict, book.StatusCode);
    }

    private sealed record CustomerRow(Guid Id, string FullName, string? Phone);
}
