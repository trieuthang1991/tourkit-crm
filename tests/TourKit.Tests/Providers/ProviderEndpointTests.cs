using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Application.Common;
using TourKit.Application.Providers.Dtos;
using TourKit.Tests.Support;

using TourKit.Shared.Enums;

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

        var createResponse = await client.PostAsJsonAsync("/api/v1/providers", new CreateProviderDto(
            "NCC-1", "Khách sạn ABC", ProviderType.Hotel, "0900000000", "abc@ncc.vn", "123 Đường A",
            "0101234567", "Nguyen Van B", "1234567890", "Vietcombank", null, 4, 1));
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<ProviderDto>();
        Assert.Equal("NCC-1", created!.Code);
        Assert.Equal(ProviderType.Hotel, created.Type);

        var list = await client.GetFromJsonAsync<PagedResult<ProviderDto>>("/api/v1/providers");
        Assert.Single(list!.Items);

        var fetched = await client.GetFromJsonAsync<ProviderDto>($"/api/v1/providers/{created.Id}");
        Assert.Equal(created.Id, fetched!.Id);

        var updateResponse = await client.PutAsJsonAsync($"/api/v1/providers/{created.Id}", new UpdateProviderDto(
            "Khách sạn ABC (mới)", ProviderType.Hotel, "0911111111", "abc2@ncc.vn", "456 Đường B",
            "0101234567", "Tran Thi C", "1234567890", "Vietcombank", null, 5, 1));
        Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

        var afterUpdate = await client.GetFromJsonAsync<ProviderDto>($"/api/v1/providers/{created.Id}");
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

        var response = await client.PostAsJsonAsync("/api/v1/providers", new CreateProviderDto(
            "", "Không mã", ProviderType.Other, null, null, null, null, null, null, null, null, 0, 1));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Providers_isolated_between_tenants()
    {
        var clientA = await LoggedInClientAsync("prov-iso-a");
        await clientA.PostAsJsonAsync("/api/v1/providers", new CreateProviderDto(
            "P-A", "NCC A", ProviderType.Vehicle, null, null, null, null, null, null, null, null, 0, 1));

        var clientB = await LoggedInClientAsync("prov-iso-b");
        var listB = await clientB.GetFromJsonAsync<PagedResult<ProviderDto>>("/api/v1/providers");
        Assert.Empty(listB!.Items);
    }

    private sealed record ProviderStats(int Total, int Active, int Inactive);

    [Fact]
    public async Task Providers_filter_by_type_and_stats()
    {
        var client = await LoggedInClientAsync("prov-filter");
        await client.PostAsJsonAsync("/api/v1/providers", new CreateProviderDto(
            "H1", "Khách sạn ABC", ProviderType.Hotel, null, null, null, null, null, null, null, null, 0, 1));
        await client.PostAsJsonAsync("/api/v1/providers", new CreateProviderDto(
            "V1", "Vận tải XYZ", ProviderType.Vehicle, null, null, null, null, null, null, null, null, 0, 0));

        var hotels = await client.GetFromJsonAsync<PagedResult<ProviderDto>>(
            $"/api/v1/providers?type={(int)ProviderType.Hotel}");
        Assert.Equal("H1", Assert.Single(hotels!.Items).Code);

        var byName = await client.GetFromJsonAsync<PagedResult<ProviderDto>>(
            "/api/v1/providers?q=" + Uri.EscapeDataString("XYZ"));
        Assert.Equal("V1", Assert.Single(byName!.Items).Code);

        var stats = await client.GetFromJsonAsync<ProviderStats>("/api/v1/providers/stats");
        Assert.Equal(2, stats!.Total);
        Assert.Equal(1, stats.Active);
        Assert.Equal(1, stats.Inactive);
    }
}
