using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TourKit.Api.Auth;
using TourKit.Api.Booking;
using TourKit.Api.Catalog;
using TourKit.Api.Finance;
using TourKit.Shared.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Tests.Support;

namespace TourKit.Tests.Finance;

public class ReceiptApprovalTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public ReceiptApprovalTests(AuthTestFactory factory) => _factory = factory;

    private async Task<(HttpClient client, string slug)> LoggedInClientAsync(string slug)
    {
        var (s, email, password) = await _factory.SeedTenantUserAsync(slug);
        var client = _factory.CreateClient();
        var auth = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(s, email, password))).Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return (client, s);
    }

    // Dựng 1 đơn 13tr (2 người lớn 5tr + 1 trẻ em 3tr) và trả về orderId.
    private static async Task<Guid> CreateOrderAsync(HttpClient client)
    {
        var tpl = await (await client.PostAsJsonAsync("/api/v1/tour-templates", new
        {
            Code = "TPL", Title = "Tour", TourType = (string?)null, TotalSlots = 30, ReservationHours = 24,
            PriceAdult = 5_000_000m, PriceChild = 3_000_000m, PriceChildSmall = 0m, PriceBaby = 0m,
            TermsNote = (string?)null,
        })).Content.ReadFromJsonAsync<TourTemplateResponse>();

        var cus = await (await client.PostAsJsonAsync("/api/v1/customers",
            new { FullName = "A", Phone = (string?)null })).Content.ReadFromJsonAsync<CustomerRow>();

        var dep = await (await client.PostAsJsonAsync("/api/v1/tour-departures", new
        {
            TemplateId = tpl!.Id, Code = "DEP", Title = "Chuyến",
            DepartureDate = (DateTimeOffset?)null, EndDate = (DateTimeOffset?)null, TotalSlots = 30,
        })).Content.ReadFromJsonAsync<DepartureResponse>();

        var order = await (await client.PostAsJsonAsync($"/api/v1/tour-departures/{dep!.Id}/bookings",
            new CreateBookingRequest(cus!.Id, 2, 1, 0, 0))).Content.ReadFromJsonAsync<OrderResponse>();
        return order!.Id;
    }

    private async Task<Guid> GetAdminUserIdAsync(string slug)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.IgnoreQueryFilters()
            .FirstAsync(u => u.Email == $"admin@{slug}.com");
        return user.Id;
    }

    private static async Task<Guid> CreateReceiptAsync(HttpClient client, Guid orderId, decimal amount)
    {
        var receipt = await (await client.PostAsJsonAsync($"/api/v1/orders/{orderId}/receipts",
            new CreateReceiptRequest(amount, "cash", null, null))).Content.ReadFromJsonAsync<ReceiptResponse>();
        return receipt!.Id;
    }

    [Fact]
    public async Task Method_one_two_steps_advances_then_approves_and_recognizes_voucher()
    {
        var (client, slug) = await LoggedInClientAsync("appr-one");
        var orderId = await CreateOrderAsync(client);
        var receiptId = await CreateReceiptAsync(client, orderId, 5_000_000m);
        var adminId = await GetAdminUserIdAsync(slug);

        var start = await client.PostAsJsonAsync($"/api/v1/receipts/{receiptId}/approval",
            new StartApprovalRequest(ApprovalMethod.One,
            [
                new ApprovalStep(1, [adminId]),
                new ApprovalStep(2, [adminId]),
            ]));
        Assert.Equal(HttpStatusCode.Created, start.StatusCode);

        var initial = await client.GetFromJsonAsync<ApprovalResponse>($"/api/v1/receipts/{receiptId}/approval");
        Assert.Equal(ApprovalStatus.InProgress, initial!.Status);
        Assert.Equal(1, initial.CurrentStepOrder);

        // Duyệt bước 1 → tiến sang bước 2, voucher vẫn CHƯA ghi nhận → công nợ chưa đổi.
        var act1 = await client.PostAsJsonAsync($"/api/v1/receipts/{receiptId}/approval/act",
            new ActRequest(true, "ok bước 1"));
        Assert.Equal(HttpStatusCode.OK, act1.StatusCode);
        var afterStep1 = await act1.Content.ReadFromJsonAsync<ApprovalResponse>();
        Assert.Equal(ApprovalStatus.InProgress, afterStep1!.Status);
        Assert.Equal(2, afterStep1.CurrentStepOrder);

        var balanceMid = await client.GetFromJsonAsync<OrderBalanceResponse>($"/api/v1/orders/{orderId}/balance");
        Assert.Equal(0m, balanceMid!.Paid);

        // Duyệt bước 2 (bước cuối) → Approved + voucher ghi nhận → công nợ phản ánh phiếu thu.
        var act2 = await client.PostAsJsonAsync($"/api/v1/receipts/{receiptId}/approval/act",
            new ActRequest(true, "ok bước 2"));
        Assert.Equal(HttpStatusCode.OK, act2.StatusCode);
        var afterStep2 = await act2.Content.ReadFromJsonAsync<ApprovalResponse>();
        Assert.Equal(ApprovalStatus.Approved, afterStep2!.Status);

        var balanceFinal = await client.GetFromJsonAsync<OrderBalanceResponse>($"/api/v1/orders/{orderId}/balance");
        Assert.Equal(5_000_000m, balanceFinal!.Paid);
    }

    [Fact]
    public async Task Reject_at_first_step_terminates_approval_without_recognizing_voucher()
    {
        var (client, slug) = await LoggedInClientAsync("appr-rej");
        var orderId = await CreateOrderAsync(client);
        var receiptId = await CreateReceiptAsync(client, orderId, 5_000_000m);
        var adminId = await GetAdminUserIdAsync(slug);

        await client.PostAsJsonAsync($"/api/v1/receipts/{receiptId}/approval",
            new StartApprovalRequest(ApprovalMethod.One,
            [
                new ApprovalStep(1, [adminId]),
                new ApprovalStep(2, [adminId]),
            ]));

        var act = await client.PostAsJsonAsync($"/api/v1/receipts/{receiptId}/approval/act",
            new ActRequest(false, "sai số tiền"));
        Assert.Equal(HttpStatusCode.OK, act.StatusCode);
        var result = await act.Content.ReadFromJsonAsync<ApprovalResponse>();
        Assert.Equal(ApprovalStatus.Rejected, result!.Status);

        var balance = await client.GetFromJsonAsync<OrderBalanceResponse>($"/api/v1/orders/{orderId}/balance");
        Assert.Equal(0m, balance!.Paid);
    }

    [Fact]
    public async Task Starting_approval_twice_for_same_receipt_is_409()
    {
        var (client, slug) = await LoggedInClientAsync("appr-dup");
        var orderId = await CreateOrderAsync(client);
        var receiptId = await CreateReceiptAsync(client, orderId, 5_000_000m);
        var adminId = await GetAdminUserIdAsync(slug);

        var body = new StartApprovalRequest(ApprovalMethod.One, [new ApprovalStep(1, [adminId])]);
        (await client.PostAsJsonAsync($"/api/v1/receipts/{receiptId}/approval", body)).EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync($"/api/v1/receipts/{receiptId}/approval", body);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Approval_is_isolated_between_tenants()
    {
        var (clientA, slugA) = await LoggedInClientAsync("appr-iso-a");
        var orderIdA = await CreateOrderAsync(clientA);
        var receiptIdA = await CreateReceiptAsync(clientA, orderIdA, 1_000_000m);
        var adminIdA = await GetAdminUserIdAsync(slugA);
        (await clientA.PostAsJsonAsync($"/api/v1/receipts/{receiptIdA}/approval",
            new StartApprovalRequest(ApprovalMethod.One, [new ApprovalStep(1, [adminIdA])])))
            .EnsureSuccessStatusCode();

        var (clientB, _) = await LoggedInClientAsync("appr-iso-b");
        var res = await clientB.GetAsync($"/api/v1/receipts/{receiptIdA}/approval");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    private sealed record CustomerRow(Guid Id, string FullName, string? Phone);
}
