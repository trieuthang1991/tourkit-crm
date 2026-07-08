using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Api.Authz;
using TourKit.Api.Booking;
using TourKit.Api.Catalog;
using TourKit.Tests.Support;

namespace TourKit.Tests.Booking;

public class SeatFlowTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public SeatFlowTests(AuthTestFactory factory) => _factory = factory;

    private static void SetBearer(HttpClient client, AuthResponse auth) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

    private static async Task<AuthResponse> LoginAsync(HttpClient client, (string slug, string email, string password) seed)
    {
        var auth = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(seed.slug, seed.email, seed.password))).Content.ReadFromJsonAsync<AuthResponse>();
        return auth!;
    }

    // Tạo mẫu (giá NL 5tr) + khách + chuyến; trả về departureId.
    private static async Task<Guid> SetupDepartureAsync(HttpClient client)
    {
        var tpl = await (await client.PostAsJsonAsync("/api/v1/tour-templates", new
        {
            Code = "TPL", Title = "Tour", TourType = (string?)null, TotalSlots = 30, ReservationHours = 24,
            PriceAdult = 5_000_000m, PriceChild = 0m, PriceChildSmall = 0m, PriceBaby = 0m, TermsNote = (string?)null,
        })).Content.ReadFromJsonAsync<TourTemplateResponse>();
        var dep = await (await client.PostAsJsonAsync("/api/v1/tour-departures", new
        {
            TemplateId = tpl!.Id, Code = "DEP", Title = "Chuyến",
            DepartureDate = (DateTimeOffset?)null, EndDate = (DateTimeOffset?)null, TotalSlots = 30,
        })).Content.ReadFromJsonAsync<DepartureResponse>();
        return dep!.Id;
    }

    private static async Task<Guid> CustomerAsync(HttpClient client) =>
        (await (await client.PostAsJsonAsync("/api/v1/customers",
            new { FullName = "A", Phone = (string?)null })).Content.ReadFromJsonAsync<CustomerRow>())!.Id;

    [Fact]
    public async Task Hold_then_confirm_then_deposit_walks_the_status_ladder()
    {
        var seed = await _factory.SeedTenantUserAsync("seat-a");
        var client = _factory.CreateClient();
        SetBearer(client, await LoginAsync(client, seed));

        var depId = await SetupDepartureAsync(client);
        var cusId = await CustomerAsync(client);

        // Giữ chỗ (1 người lớn, giá 5tr) → Held + có đếm ngược
        var held = await (await client.PostAsJsonAsync($"/api/v1/tour-departures/{depId}/holds",
            new CreateBookingRequest(cusId, 1, 0, 0, 0))).Content.ReadFromJsonAsync<SeatResponse>();
        Assert.Equal(SeatStatus.Held, held!.Status);
        Assert.NotNull(held.HoldExpiresAt);
        Assert.Equal(5_000_000m, held.LineTotal);

        // Xác nhận chỗ → HeldConfirmed, hết đếm ngược
        var confirmed = await (await client.PostAsync($"/api/v1/tour-customers/{held.Id}/confirm-seat", null))
            .Content.ReadFromJsonAsync<SeatResponse>();
        Assert.Equal(SeatStatus.HeldConfirmed, confirmed!.Status);
        Assert.Null(confirmed.HoldExpiresAt);

        // Đặt cọc 2tr → Deposited
        var deposited = await (await client.PostAsJsonAsync($"/api/v1/tour-customers/{held.Id}/deposit",
            new DepositRequest(2_000_000m))).Content.ReadFromJsonAsync<SeatResponse>();
        Assert.Equal(SeatStatus.Deposited, deposited!.Status);

        // Cọc thêm 3tr (đủ 5tr) → Paid
        var paid = await (await client.PostAsJsonAsync($"/api/v1/tour-customers/{held.Id}/deposit",
            new DepositRequest(3_000_000m))).Content.ReadFromJsonAsync<SeatResponse>();
        Assert.Equal(SeatStatus.Paid, paid!.Status);
    }

    [Fact]
    public async Task Cancel_seat_marks_cancelled_and_blocks_double_cancel()
    {
        var seed = await _factory.SeedTenantUserAsync("seat-cancel");
        var client = _factory.CreateClient();
        SetBearer(client, await LoginAsync(client, seed));

        var depId = await SetupDepartureAsync(client);
        var cusId = await CustomerAsync(client);

        var held = await (await client.PostAsJsonAsync($"/api/v1/tour-departures/{depId}/holds",
            new CreateBookingRequest(cusId, 1, 0, 0, 0))).Content.ReadFromJsonAsync<SeatResponse>();
        await client.PostAsJsonAsync($"/api/v1/tour-customers/{held!.Id}/deposit", new DepositRequest(2_000_000m));

        // huỷ + hoàn 1tr
        var cancel = await client.PostAsJsonAsync($"/api/v1/tour-customers/{held.Id}/cancel",
            new CancelSeatRequest("Khách đổi lịch", 1_000_000m));
        Assert.Equal(HttpStatusCode.OK, cancel.StatusCode);
        var seat = await cancel.Content.ReadFromJsonAsync<SeatResponse>();
        Assert.Equal(SeatStatus.Cancelled, seat!.Status);

        // huỷ lần 2 → 409
        var again = await client.PostAsJsonAsync($"/api/v1/tour-customers/{held.Id}/cancel",
            new CancelSeatRequest(null, 0m));
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Confirm_seat_forbidden_without_permission()
    {
        // đủ quyền dựng + giữ chỗ, KHÔNG có booking.seat.confirm
        var seed = await _factory.SeedTenantUserWithPermissionsAsync("seat-noperm",
            Permissions.TourCreate, Permissions.CustomerCreate,
            Permissions.DepartureCreate, Permissions.BookingCreate, Permissions.BookingView);
        var client = _factory.CreateClient();
        SetBearer(client, await LoginAsync(client, seed));

        var depId = await SetupDepartureAsync(client);
        var cusId = await CustomerAsync(client);
        var held = await (await client.PostAsJsonAsync($"/api/v1/tour-departures/{depId}/holds",
            new CreateBookingRequest(cusId, 1, 0, 0, 0))).Content.ReadFromJsonAsync<SeatResponse>();

        var res = await client.PostAsync($"/api/v1/tour-customers/{held!.Id}/confirm-seat", null);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    private sealed record CustomerRow(Guid Id, string FullName, string? Phone);
}
