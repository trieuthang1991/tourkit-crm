using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Application.Settings;

namespace TourKit.Tests.Support;

/// <summary>Hồ sơ công ty (legacy Config) qua /api/v1/company-profile — singleton mỗi tenant.</summary>
public class CompanyProfileTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public CompanyProfileTests(AuthTestFactory factory) => _factory = factory;

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
    public async Task Get_default_empty_then_save_and_read_back()
    {
        var client = await LoggedInClientAsync("company-a");

        var initial = await client.GetFromJsonAsync<CompanyProfileDto>("/api/v1/company-profile");
        Assert.Equal(string.Empty, initial!.Name);

        var save = await client.PutAsJsonAsync("/api/v1/company-profile", new CompanyProfileDto(
            "Công ty Du lịch Biển Xanh", "Biển Xanh", "45 Trần Phú, Nha Trang", "1900 8888",
            "info@bienxanh.vn", "bienxanh.vn", "4200123456", "Trần Thị B", "Tổng giám đốc",
            "GP-88/2020/TCDL-GPLHQT", "Vietcombank 0061001234567"));
        Assert.Equal(HttpStatusCode.NoContent, save.StatusCode);

        var saved = await client.GetFromJsonAsync<CompanyProfileDto>("/api/v1/company-profile");
        Assert.Equal("Công ty Du lịch Biển Xanh", saved!.Name);
        Assert.Equal("4200123456", saved.TaxCode);
        Assert.Equal("Trần Thị B", saved.LegalRepName);
    }
}
