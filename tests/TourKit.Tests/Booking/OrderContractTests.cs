using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Catalog.Dtos;
using TourKit.Tests.Support;

namespace TourKit.Tests.Booking;

/// <summary>Dữ liệu in hợp đồng tour (legacy contract_tour): gom đơn + khách + chuyến + điều khoản.</summary>
public class OrderContractTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public OrderContractTests(AuthTestFactory factory) => _factory = factory;

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
    public async Task Contract_assembles_customer_tour_pax_and_price()
    {
        var client = await LoggedInClientAsync("contract-a");
        var tpl = await (await client.PostAsJsonAsync("/api/v1/tour-templates", new
        {
            Code = "TPL-CT", Title = "Đà Nẵng 3N2Đ", TourType = (string?)null, TotalSlots = 30, ReservationHours = 24,
            PriceAdult = 5_000_000m, PriceChild = 3_000_000m, PriceChildSmall = 0m, PriceBaby = 0m,
            TermsNote = "Thanh toán 100% trước khởi hành 7 ngày.",
        })).Content.ReadFromJsonAsync<TourTemplateDto>();
        var customer = await (await client.PostAsJsonAsync("/api/v1/customers",
            new { FullName = "Nguyễn Văn A", Phone = "0900000000" })).Content.ReadFromJsonAsync<CustomerRow>();
        var dep = await (await client.PostAsJsonAsync("/api/v1/tour-departures", new
        {
            TemplateId = tpl!.Id, Code = "DEP-CT", Title = "Đà Nẵng chuyến 1",
            DepartureDate = (DateTimeOffset?)null, EndDate = (DateTimeOffset?)null, TotalSlots = 30,
        })).Content.ReadFromJsonAsync<DepartureDto>();
        var order = await (await client.PostAsJsonAsync($"/api/v1/tour-departures/{dep!.Id}/bookings",
            new CreateBookingDto(customer!.Id, 2, 1, 0, 0))).Content.ReadFromJsonAsync<OrderDto>();

        var contract = await client.GetFromJsonAsync<OrderContractDto>($"/api/v1/orders/{order!.Id}/contract");

        Assert.Equal("Nguyễn Văn A", contract!.CustomerName);
        Assert.Equal("Đà Nẵng chuyến 1", contract.TourTitle);
        Assert.Equal(2, contract.AdultCount);
        Assert.Equal(1, contract.ChildCount);
        Assert.Equal(13_000_000m, contract.TotalRevenue);   // 2×5tr + 1×3tr
        Assert.Equal("Thanh toán 100% trước khởi hành 7 ngày.", contract.Terms);
    }
}
