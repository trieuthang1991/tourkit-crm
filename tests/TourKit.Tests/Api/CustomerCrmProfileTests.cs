using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Application.Common;
using TourKit.Tests.Support;

namespace TourKit.Tests.Api;

/// <summary>Mở rộng CRM Customer bám hệ cũ: Code tự sinh + field/list lưu qua CrmProfileJson round-trip.</summary>
public class CustomerCrmProfileTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public CustomerCrmProfileTests(AuthTestFactory factory) => _factory = factory;

    private async Task<HttpClient> LoggedInClientAsync(string slug)
    {
        var (s, email, password) = await _factory.SeedTenantUserAsync(slug);
        var client = _factory.CreateClient();
        var auth = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(s, email, password))).Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    private sealed record CrmCustomerRow(
        Guid Id, string? Code, string FullName, int CustomerType,
        string? Gender, string? City, string? Campaign,
        List<string> Segments, List<string> Tags, List<string> AssignedTo, List<string> AssignedToNames,
        string? CreatedBy, int PurchaseCount, decimal Revenue);

    [Fact]
    public async Task Create_customer_persists_crm_lists_and_auto_code()
    {
        var client = await LoggedInClientAsync("crm-a");

        (await client.PostAsJsonAsync("/api/v1/customers", new
        {
            FullName = "Nguyễn Văn A",
            Phone = "0900000000",
            CustomerType = 1,
            Gender = "Nam",
            City = "Đà Nẵng",
            Campaign = "Hè 2026",
            Segments = new[] { "B2B", "VIP" },
            Tags = new[] { "Nóng" },
            AssignedTo = new[] { "legacy-staff-01" }, // ID legacy dạng string → vẫn lưu được
        })).EnsureSuccessStatusCode();

        var list = await client.GetFromJsonAsync<PagedResult<CrmCustomerRow>>("/api/v1/customers");
        var row = Assert.Single(list!.Items);

        Assert.StartsWith("KH_", row.Code);
        Assert.Equal("Nam", row.Gender);
        Assert.Equal("Đà Nẵng", row.City);
        Assert.Equal(new[] { "B2B", "VIP" }, row.Segments);
        Assert.Equal(new[] { "Nóng" }, row.Tags);
        Assert.Equal(new[] { "legacy-staff-01" }, row.AssignedTo);
        // ID legacy không khớp user GUID → tên giữ nguyên chuỗi (không mất dữ liệu khi migrate).
        Assert.Equal(new[] { "legacy-staff-01" }, row.AssignedToNames);
        Assert.False(string.IsNullOrEmpty(row.CreatedBy)); // set từ current user
        Assert.Equal(0, row.PurchaseCount);
    }
}
