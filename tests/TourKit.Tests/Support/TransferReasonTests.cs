using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Application.Catalog.Dtos;

namespace TourKit.Tests.Support;

/// <summary>Danh mục lý do chuyển chuyến (legacy ReasonSwitch) qua /api/v1/transfer-reasons.</summary>
public class TransferReasonTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public TransferReasonTests(AuthTestFactory factory) => _factory = factory;

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
    public async Task Create_and_list_transfer_reasons()
    {
        var client = await LoggedInClientAsync("transfer-reason-a");

        var created = await (await client.PostAsJsonAsync("/api/v1/transfer-reasons",
            new CreateTransferReasonDto("Khách đổi lịch", 1))).Content.ReadFromJsonAsync<TransferReasonDto>();
        Assert.Equal("Khách đổi lịch", created!.Name);

        var list = await client.GetFromJsonAsync<List<TransferReasonDto>>("/api/v1/transfer-reasons");
        Assert.Single(list!);

        var del = await client.DeleteAsync($"/api/v1/transfer-reasons/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);
    }
}
