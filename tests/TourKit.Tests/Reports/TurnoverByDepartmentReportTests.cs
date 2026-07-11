using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Application.Admin;
using TourKit.Application.Booking.Dtos;
using TourKit.Application.Catalog.Dtos;
using TourKit.Application.Reports.Dtos;
using TourKit.Tests.Support;

namespace TourKit.Tests.Reports;

/// <summary>Báo cáo doanh thu/lợi nhuận theo phòng ban — gom đơn theo phòng ban của sales phụ trách.</summary>
public class TurnoverByDepartmentReportTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public TurnoverByDepartmentReportTests(AuthTestFactory factory) => _factory = factory;

    private async Task<HttpClient> LoggedInClientAsync(string slug)
    {
        var (s, email, password) = await _factory.SeedTenantUserAsync(slug);
        var client = _factory.CreateClient();
        var auth = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(s, email, password))).Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

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
        return order!.Id;   // doanh thu 13tr
    }

    [Fact]
    public async Task Turnover_grouped_by_sales_department()
    {
        var client = await LoggedInClientAsync("tbd-a");

        // Admin user (seed) → gán vào phòng "Điều hành".
        var users = await client.GetFromJsonAsync<List<UserListDto>>("/api/v1/users");
        var admin = users!.Single();
        var dept = await (await client.PostAsJsonAsync("/api/v1/departments",
            new CreateDepartmentDto("Điều hành", null, 1))).Content.ReadFromJsonAsync<DepartmentDto>();
        (await client.PutAsJsonAsync($"/api/v1/users/{admin.Id}/org", new AssignUserOrgDto(dept!.Id, null)))
            .EnsureSuccessStatusCode();

        // 1 đơn gán admin làm sales, 1 đơn không gán sales (→ "Chưa phân bổ").
        var withSales = await CreateOrderAsync(client);
        (await client.PutAsJsonAsync($"/api/v1/orders/{withSales}/sales", new AssignSalesDto(admin.Id)))
            .EnsureSuccessStatusCode();
        await CreateOrderAsync(client);

        var rows = await client.GetFromJsonAsync<List<TurnoverByDepartmentRowDto>>("/api/v1/reports/turnover-by-department");
        Assert.NotNull(rows);

        var dh = rows.Single(r => r.DepartmentId == dept.Id);
        Assert.Equal("Điều hành", dh.DepartmentName);
        Assert.Equal(1, dh.OrderCount);
        Assert.Equal(13_000_000m, dh.Turnover);

        var unassigned = rows.Single(r => r.DepartmentId == null);
        Assert.Equal("Chưa phân bổ", unassigned.DepartmentName);
        Assert.Equal(1, unassigned.OrderCount);
    }

    private sealed record CustomerRow(Guid Id, string FullName, string? Phone);
}
