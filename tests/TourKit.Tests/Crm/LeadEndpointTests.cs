using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Application.Common;
using TourKit.Application.Crm.Dtos;
using TourKit.Tests.Support;

namespace TourKit.Tests.Crm;

public class LeadEndpointTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public LeadEndpointTests(AuthTestFactory factory) => _factory = factory;

    private async Task<HttpClient> LoggedInClientAsync(string slug)
    {
        var (s, email, password) = await _factory.SeedTenantUserAsync(slug);
        var client = _factory.CreateClient();
        var auth = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(s, email, password))).Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    private static CreateLeadDto Sample(string name) =>
        new(name, "0900000000", $"{name}@x.com", "facebook", null);

    [Fact]
    public async Task Create_list_then_convert_to_customer()
    {
        var client = await LoggedInClientAsync("lead-a");

        var created = await client.PostAsJsonAsync("/api/v1/leads", Sample("Nguyen Van A"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var lead = await created.Content.ReadFromJsonAsync<LeadDto>();
        Assert.NotNull(lead);

        var list = await client.GetFromJsonAsync<PagedResult<LeadDto>>("/api/v1/leads");
        Assert.Single(list!.Items);

        // convert → tạo Customer
        var conv = await client.PostAsync($"/api/v1/leads/{lead!.Id}/convert", null);
        Assert.Equal(HttpStatusCode.Created, conv.StatusCode);
        var convBody = await conv.Content.ReadFromJsonAsync<ConvertLeadResultDto>();
        Assert.NotNull(convBody);

        // Customer xuất hiện ở /customers
        var customers = await client.GetFromJsonAsync<PagedResult<CustomerRow>>("/api/v1/customers");
        Assert.Contains(customers!.Items, c => c.Id == convBody!.CustomerId && c.FullName == "Nguyen Van A");

        // convert lần 2 → 409
        var again = await client.PostAsync($"/api/v1/leads/{lead.Id}/convert", null);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Isolated_between_tenants()
    {
        var clientA = await LoggedInClientAsync("lead-iso-a");
        await clientA.PostAsJsonAsync("/api/v1/leads", Sample("A"));

        var clientB = await LoggedInClientAsync("lead-iso-b");
        var listB = await clientB.GetFromJsonAsync<PagedResult<LeadDto>>("/api/v1/leads");
        Assert.Empty(listB!.Items);
    }

    private sealed record CustomerRow(Guid Id, string FullName, string? Phone);
}
