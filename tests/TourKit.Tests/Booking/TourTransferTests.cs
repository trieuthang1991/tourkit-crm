using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Common;
using TourKit.Tests.Support;

namespace TourKit.Tests.Booking;

/// <summary>Chuyển chuyến (legacy TransferHistory): dời đơn + chỗ sang chuyến khác, giữ giá, ghi lịch sử.</summary>
public class TourTransferTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public TourTransferTests(AuthTestFactory factory) => _factory = factory;

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

    private static async Task<Guid> CreateDepartureAsync(HttpClient client, Guid templateId, string code, int slots)
    {
        var dep = await (await client.PostAsJsonAsync("/api/v1/tour-departures", new
        {
            TemplateId = templateId, Code = code, Title = "Chuyến " + code,
            DepartureDate = (DateTimeOffset?)null, EndDate = (DateTimeOffset?)null, TotalSlots = slots,
        })).Content.ReadFromJsonAsync<DepartureDto>();
        return dep!.Id;
    }

    [Fact]
    public async Task Transfer_moves_order_to_target_departure_and_records_history()
    {
        var client = await LoggedInClientAsync("transfer-a");
        var tpl = await (await client.PostAsJsonAsync("/api/v1/tour-templates", new
        {
            Code = "TPL-TR", Title = "Tour", TourType = (string?)null, TotalSlots = 30, ReservationHours = 24,
            PriceAdult = 5_000_000m, PriceChild = 3_000_000m, PriceChildSmall = 0m, PriceBaby = 0m, TermsNote = (string?)null,
        })).Content.ReadFromJsonAsync<TourTemplateDto>();
        var customer = await (await client.PostAsJsonAsync("/api/v1/customers",
            new { FullName = "KH", Phone = (string?)null })).Content.ReadFromJsonAsync<CustomerRow>();

        var depA = await CreateDepartureAsync(client, tpl!.Id, "DEP-A", 30);
        var depB = await CreateDepartureAsync(client, tpl.Id, "DEP-B", 30);

        var order = await (await client.PostAsJsonAsync($"/api/v1/tour-departures/{depA}/bookings",
            new CreateBookingDto(customer!.Id, 2, 1, 0, 0))).Content.ReadFromJsonAsync<OrderDto>();
        var revenueBefore = order!.TotalRevenue;

        var transfer = await client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/transfers",
            new TransferOrderDto(depB, "Khách kẹt lịch"));
        Assert.Equal(HttpStatusCode.Created, transfer.StatusCode);

        // Đơn đã sang chuyến B, giá không đổi (đọc từ danh sách đơn — không có GET đơn lẻ).
        var orders = await client.GetFromJsonAsync<PagedResult<OrderDto>>("/api/v1/orders?page=1&size=200");
        var reloaded = orders!.Items.Single(o => o.Id == order.Id);
        Assert.Equal(depB, reloaded.TourDepartureId);
        Assert.Equal(revenueBefore, reloaded.TotalRevenue);

        // Lịch sử chuyển ghi đúng nguồn/đích + lý do.
        var history = await client.GetFromJsonAsync<List<TourTransferDto>>($"/api/v1/orders/{order.Id}/transfers");
        Assert.Single(history!);
        Assert.Equal(depA, history![0].FromDepartureId);
        Assert.Equal(depB, history[0].ToDepartureId);
        Assert.Equal("Khách kẹt lịch", history[0].Reason);
    }

    [Fact]
    public async Task Transfer_to_full_departure_returns_409()
    {
        var client = await LoggedInClientAsync("transfer-full");
        var tpl = await (await client.PostAsJsonAsync("/api/v1/tour-templates", new
        {
            Code = "TPL-TRF", Title = "Tour", TourType = (string?)null, TotalSlots = 3, ReservationHours = 24,
            PriceAdult = 5_000_000m, PriceChild = 3_000_000m, PriceChildSmall = 0m, PriceBaby = 0m, TermsNote = (string?)null,
        })).Content.ReadFromJsonAsync<TourTemplateDto>();
        var c1 = await (await client.PostAsJsonAsync("/api/v1/customers", new { FullName = "A", Phone = (string?)null })).Content.ReadFromJsonAsync<CustomerRow>();
        var c2 = await (await client.PostAsJsonAsync("/api/v1/customers", new { FullName = "B", Phone = (string?)null })).Content.ReadFromJsonAsync<CustomerRow>();

        var depA = await CreateDepartureAsync(client, tpl!.Id, "SA", 30);
        var depB = await CreateDepartureAsync(client, tpl.Id, "SB", 2);   // đích chỉ 2 chỗ

        // Đơn ở A có 3 khách; B đã có 1 khách → còn 1 chỗ, không đủ.
        var order = await (await client.PostAsJsonAsync($"/api/v1/tour-departures/{depA}/bookings",
            new CreateBookingDto(c1!.Id, 3, 0, 0, 0))).Content.ReadFromJsonAsync<OrderDto>();
        await client.PostAsJsonAsync($"/api/v1/tour-departures/{depB}/bookings", new CreateBookingDto(c2!.Id, 1, 0, 0, 0));

        var transfer = await client.PostAsJsonAsync($"/api/v1/orders/{order!.Id}/transfers", new TransferOrderDto(depB, null));

        Assert.Equal(HttpStatusCode.Conflict, transfer.StatusCode);
    }

    [Fact]
    public async Task Transfer_to_same_departure_returns_400()
    {
        var client = await LoggedInClientAsync("transfer-same");
        var tpl = await (await client.PostAsJsonAsync("/api/v1/tour-templates", new
        {
            Code = "TPL-TRS", Title = "Tour", TourType = (string?)null, TotalSlots = 30, ReservationHours = 24,
            PriceAdult = 5_000_000m, PriceChild = 3_000_000m, PriceChildSmall = 0m, PriceBaby = 0m, TermsNote = (string?)null,
        })).Content.ReadFromJsonAsync<TourTemplateDto>();
        var customer = await (await client.PostAsJsonAsync("/api/v1/customers", new { FullName = "KH", Phone = (string?)null })).Content.ReadFromJsonAsync<CustomerRow>();
        var depA = await CreateDepartureAsync(client, tpl!.Id, "SAME", 30);
        var order = await (await client.PostAsJsonAsync($"/api/v1/tour-departures/{depA}/bookings",
            new CreateBookingDto(customer!.Id, 1, 0, 0, 0))).Content.ReadFromJsonAsync<OrderDto>();

        var transfer = await client.PostAsJsonAsync($"/api/v1/orders/{order!.Id}/transfers", new TransferOrderDto(depA, null));

        Assert.Equal(HttpStatusCode.BadRequest, transfer.StatusCode);
    }
}
