using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Api.Booking;
using TourKit.Api.Catalog;
using TourKit.Tests.Support;

namespace TourKit.Tests.Booking;

public class BookingEndpointTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public BookingEndpointTests(AuthTestFactory factory) => _factory = factory;

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
    public async Task Open_departure_then_book_customer_computes_total()
    {
        var client = await LoggedInClientAsync("book-a");

        // mẫu tour (giá người lớn 5tr, trẻ em 3tr)
        var template = await (await client.PostAsJsonAsync("/api/v1/tour-templates", new
        {
            Code = "TPL-1", Title = "Đà Nẵng", TourType = (string?)null, TotalSlots = 30, ReservationHours = 24,
            PriceAdult = 5_000_000m, PriceChild = 3_000_000m, PriceChildSmall = 0m, PriceBaby = 0m,
            TermsNote = (string?)null,
        })).Content.ReadFromJsonAsync<TourTemplateResponse>();

        // khách
        var customer = await (await client.PostAsJsonAsync("/api/v1/customers",
            new { FullName = "Nguyen Van A", Phone = (string?)null }))
            .Content.ReadFromJsonAsync<CustomerRow>();

        // mở chuyến từ mẫu
        var departure = await (await client.PostAsJsonAsync("/api/v1/tour-departures", new
        {
            TemplateId = template!.Id, Code = "DEP-1", Title = "Đà Nẵng 20/07",
            DepartureDate = DateTimeOffset.UtcNow.AddDays(30), EndDate = (DateTimeOffset?)null, TotalSlots = 30,
        })).Content.ReadFromJsonAsync<DepartureResponse>();

        // đặt 2 người lớn + 1 trẻ em → total = 2*5tr + 1*3tr = 13tr
        var booking = await client.PostAsJsonAsync($"/api/v1/tour-departures/{departure!.Id}/bookings",
            new CreateBookingRequest(customer!.Id, 2, 1, 0, 0));
        Assert.Equal(HttpStatusCode.Created, booking.StatusCode);
        var order = await booking.Content.ReadFromJsonAsync<OrderResponse>();
        Assert.Equal(13_000_000m, order!.TotalRevenue);
        Assert.Equal(customer.Id, order.CustomerId);

        var orders = await client.GetFromJsonAsync<List<OrderResponse>>("/api/v1/orders");
        Assert.Single(orders!);
    }

    [Fact]
    public async Task Booking_on_departure_without_template_is_400()
    {
        var client = await LoggedInClientAsync("book-notpl");

        var departure = await (await client.PostAsJsonAsync("/api/v1/tour-departures", new
        {
            TemplateId = (Guid?)null, Code = "DEP-X", Title = "Không mẫu",
            DepartureDate = (DateTimeOffset?)null, EndDate = (DateTimeOffset?)null, TotalSlots = 10,
        })).Content.ReadFromJsonAsync<DepartureResponse>();

        var customer = await (await client.PostAsJsonAsync("/api/v1/customers",
            new { FullName = "B", Phone = (string?)null })).Content.ReadFromJsonAsync<CustomerRow>();

        var booking = await client.PostAsJsonAsync($"/api/v1/tour-departures/{departure!.Id}/bookings",
            new CreateBookingRequest(customer!.Id, 1, 0, 0, 0));
        Assert.Equal(HttpStatusCode.BadRequest, booking.StatusCode);
    }

    [Fact]
    public async Task Orders_isolated_between_tenants()
    {
        var clientA = await LoggedInClientAsync("book-iso-a");
        var tpl = await (await clientA.PostAsJsonAsync("/api/v1/tour-templates", new
        {
            Code = "T", Title = "T", TourType = (string?)null, TotalSlots = 10, ReservationHours = 24,
            PriceAdult = 1m, PriceChild = 1m, PriceChildSmall = 0m, PriceBaby = 0m, TermsNote = (string?)null,
        })).Content.ReadFromJsonAsync<TourTemplateResponse>();
        var cus = await (await clientA.PostAsJsonAsync("/api/v1/customers",
            new { FullName = "A", Phone = (string?)null })).Content.ReadFromJsonAsync<CustomerRow>();
        var dep = await (await clientA.PostAsJsonAsync("/api/v1/tour-departures", new
        {
            TemplateId = tpl!.Id, Code = "D", Title = "D",
            DepartureDate = (DateTimeOffset?)null, EndDate = (DateTimeOffset?)null, TotalSlots = 10,
        })).Content.ReadFromJsonAsync<DepartureResponse>();
        await clientA.PostAsJsonAsync($"/api/v1/tour-departures/{dep!.Id}/bookings",
            new CreateBookingRequest(cus!.Id, 1, 0, 0, 0));

        var clientB = await LoggedInClientAsync("book-iso-b");
        var ordersB = await clientB.GetFromJsonAsync<List<OrderResponse>>("/api/v1/orders");
        Assert.Empty(ordersB!);
    }

    private sealed record CustomerRow(Guid Id, string FullName, string? Phone);
}
