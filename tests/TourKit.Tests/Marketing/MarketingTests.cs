using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Api.Marketing;
using TourKit.Shared.Entities;
using TourKit.Shared.Application;
using TourKit.Tests.Support;

namespace TourKit.Tests.Marketing;

public class MarketingTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public MarketingTests(AuthTestFactory factory) => _factory = factory;

    private async Task<HttpClient> LoggedInClientAsync(string slug)
    {
        var (s, email, password) = await _factory.SeedTenantUserAsync(slug);
        var client = _factory.CreateClient();
        var auth = await (await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(s, email, password))).Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    private static CreateCampaignRequest Sample(string name) =>
        new(name, MarketingChannel.Email, "Chào hè", "Nội dung khuyến mãi hè 2026.");

    [Fact]
    public async Task Create_campaign_then_list_and_get()
    {
        var client = await LoggedInClientAsync("mkt-a");

        var created = await client.PostAsJsonAsync("/api/v1/marketing/campaigns", Sample("Hè 2026"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var campaign = await created.Content.ReadFromJsonAsync<CampaignResponse>();
        Assert.NotNull(campaign);
        Assert.Equal("Hè 2026", campaign!.Name);
        Assert.Equal(MarketingChannel.Email, campaign.Channel);

        var list = await client.GetFromJsonAsync<Paged<CampaignResponse>>("/api/v1/marketing/campaigns");
        Assert.Single(list!.Items);

        var got = await client.GetFromJsonAsync<CampaignResponse>($"/api/v1/marketing/campaigns/{campaign.Id}");
        Assert.NotNull(got);
        Assert.Equal(campaign.Id, got!.Id);
    }

    [Fact]
    public async Task Create_with_missing_name_returns_400()
    {
        var client = await LoggedInClientAsync("mkt-invalid");

        var res = await client.PostAsJsonAsync("/api/v1/marketing/campaigns",
            new CreateCampaignRequest("", MarketingChannel.Sms, null, "Body"));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Update_campaign_changes_fields()
    {
        var client = await LoggedInClientAsync("mkt-update");

        var created = await client.PostAsJsonAsync("/api/v1/marketing/campaigns", Sample("Trước update"));
        var campaign = await created.Content.ReadFromJsonAsync<CampaignResponse>();

        var update = await client.PutAsJsonAsync($"/api/v1/marketing/campaigns/{campaign!.Id}",
            new UpdateCampaignRequest("Sau update", MarketingChannel.Zalo, "Chủ đề mới", "Nội dung mới", 2));
        Assert.Equal(HttpStatusCode.NoContent, update.StatusCode);

        var got = await client.GetFromJsonAsync<CampaignResponse>($"/api/v1/marketing/campaigns/{campaign.Id}");
        Assert.Equal("Sau update", got!.Name);
        Assert.Equal(MarketingChannel.Zalo, got.Channel);
        Assert.Equal(2, got.Status);
    }

    [Fact]
    public async Task Send_campaign_records_logs_and_updates_status()
    {
        var client = await LoggedInClientAsync("mkt-send");

        var created = await client.PostAsJsonAsync("/api/v1/marketing/campaigns", Sample("Chiến dịch gửi"));
        var campaign = await created.Content.ReadFromJsonAsync<CampaignResponse>();

        var send = await client.PostAsJsonAsync($"/api/v1/marketing/campaigns/{campaign!.Id}/send",
            new SendCampaignRequest(["a@x.com", "b@x.com"]));
        Assert.Equal(HttpStatusCode.OK, send.StatusCode);
        var result = await send.Content.ReadFromJsonAsync<SendResultResponse>();
        Assert.NotNull(result);
        Assert.Equal(2, result!.Sent);

        var logs = await client.GetFromJsonAsync<List<SendLogResponse>>(
            $"/api/v1/marketing/campaigns/{campaign.Id}/logs");
        Assert.NotNull(logs);
        Assert.Equal(2, logs!.Count);
        Assert.Contains(logs, l => l.Recipient == "a@x.com");
        Assert.Contains(logs, l => l.Recipient == "b@x.com");

        var got = await client.GetFromJsonAsync<CampaignResponse>($"/api/v1/marketing/campaigns/{campaign.Id}");
        Assert.Equal(1, got!.Status);   // đã gửi
    }

    [Fact]
    public async Task Send_to_unknown_campaign_returns_404()
    {
        var client = await LoggedInClientAsync("mkt-send-404");

        var send = await client.PostAsJsonAsync($"/api/v1/marketing/campaigns/{Guid.NewGuid()}/send",
            new SendCampaignRequest(["a@x.com"]));
        Assert.Equal(HttpStatusCode.NotFound, send.StatusCode);
    }

    [Fact]
    public async Task Soft_delete_hides_campaign_from_list()
    {
        var client = await LoggedInClientAsync("mkt-delete");

        var created = await client.PostAsJsonAsync("/api/v1/marketing/campaigns", Sample("Xoá mềm"));
        var campaign = await created.Content.ReadFromJsonAsync<CampaignResponse>();

        var del = await client.DeleteAsync($"/api/v1/marketing/campaigns/{campaign!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, del.StatusCode);

        var list = await client.GetFromJsonAsync<Paged<CampaignResponse>>("/api/v1/marketing/campaigns");
        Assert.Empty(list!.Items);

        var get = await client.GetAsync($"/api/v1/marketing/campaigns/{campaign.Id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Fact]
    public async Task Isolated_between_tenants()
    {
        var clientA = await LoggedInClientAsync("mkt-iso-a");
        await clientA.PostAsJsonAsync("/api/v1/marketing/campaigns", Sample("Của A"));

        var clientB = await LoggedInClientAsync("mkt-iso-b");
        var listB = await clientB.GetFromJsonAsync<Paged<CampaignResponse>>("/api/v1/marketing/campaigns");
        Assert.Empty(listB!.Items);
    }
}
