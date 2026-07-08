using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Api.Providers;
using TourKit.Infrastructure.Entities;
using TourKit.Tests.Support;

namespace TourKit.Tests.Providers;

public class ProviderEndpointTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public ProviderEndpointTests(AuthTestFactory factory) => _factory = factory;

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
    public async Task Create_then_list_then_get_then_update_then_delete_provider()
    {
        var client = await LoggedInClientAsync("prov-a");

        var createResponse = await client.PostAsJsonAsync("/api/v1/providers", new CreateProviderRequest(
            "NCC-1", "Khách sạn ABC", ProviderType.Hotel, "0900000000", "abc@ncc.vn", "123 Đường A",
            "0101234567", "Nguyen Van B", "1234567890", "Vietcombank", 4, 1));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<ProviderResponse>();
        Assert.Equal("NCC-1", created!.Code);
        Assert.Equal(ProviderType.Hotel, created.Type);

        var list = await client.GetFromJsonAsync<List<ProviderResponse>>("/api/v1/providers");
        Assert.Single(list!);

        var fetched = await client.GetFromJsonAsync<ProviderResponse>($"/api/v1/providers/{created.Id}");
        Assert.Equal(created.Id, fetched!.Id);

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/providers/{created.Id}", new UpdateProviderRequest(
            "Khách sạn ABC (mới)", ProviderType.Hotel, "0911111111", "abc2@ncc.vn", "456 Đường B",
            "0101234567", "Tran Thi C", "1234567890", "Vietcombank", 5, 1));
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var afterUpdate = await client.GetFromJsonAsync<ProviderResponse>($"/api/v1/providers/{created.Id}");
        Assert.Equal("Khách sạn ABC (mới)", afterUpdate!.Name);
        Assert.Equal(5, afterUpdate.Rate);

        var deleteResponse = await client.DeleteAsync($"/api/v1/providers/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var afterDelete = await client.GetAsync($"/api/v1/providers/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, afterDelete.StatusCode);
    }

    [Fact]
    public async Task Create_provider_without_code_is_400()
    {
        var client = await LoggedInClientAsync("prov-invalid");

        var response = await client.PostAsJsonAsync("/api/v1/providers", new CreateProviderRequest(
            "", "Không mã", ProviderType.Other, null, null, null, null, null, null, null, 0, 1));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Providers_isolated_between_tenants()
    {
        var clientA = await LoggedInClientAsync("prov-iso-a");
        await clientA.PostAsJsonAsync("/api/v1/providers", new CreateProviderRequest(
            "P-A", "NCC A", ProviderType.Vehicle, null, null, null, null, null, null, null, 0, 1));

        var clientB = await LoggedInClientAsync("prov-iso-b");
        var listB = await clientB.GetFromJsonAsync<List<ProviderResponse>>("/api/v1/providers");
        Assert.Empty(listB!);
    }
}
