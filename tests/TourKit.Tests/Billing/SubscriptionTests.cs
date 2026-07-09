using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TourKit.Api.Auth;
using TourKit.Api.Billing;
using TourKit.Api.Provisioning;
using TourKit.Tests.Support;

namespace TourKit.Tests.Billing;

public class SubscriptionTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public SubscriptionTests(AuthTestFactory factory) => _factory = factory;

    private static RegisterTenantRequest Sample(string slug) =>
        new("Công ty " + slug, slug, $"admin@{slug}.com", "P@ssw0rd!", "Admin");

    private async Task<HttpClient> RegisterAndLoginAsync(string slug)
    {
        var client = _factory.CreateClient();

        var reg = await client.PostAsJsonAsync("/api/v1/registration", Sample(slug));
        Assert.Equal(HttpStatusCode.Created, reg.StatusCode);

        var login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequest(slug, $"admin@{slug}.com", "P@ssw0rd!"));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var auth = await login.Content.ReadFromJsonAsync<AuthResponse>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    [Fact]
    public async Task New_tenant_has_active_subscription_on_free_plan()
    {
        var client = await RegisterAndLoginAsync("subco1");

        var res = await client.GetAsync("/api/v1/subscription");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var sub = await res.Content.ReadFromJsonAsync<SubscriptionResponse>();
        Assert.NotNull(sub);
        Assert.Equal("free", sub!.PlanCode);
        Assert.Equal(TourKit.Shared.Enums.SubscriptionStatus.Active, sub.Status);
    }

    [Fact]
    public async Task Plans_endpoint_lists_free_and_pro()
    {
        var client = await RegisterAndLoginAsync("subco2");

        var res = await client.GetAsync("/api/v1/plans");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var plans = await res.Content.ReadFromJsonAsync<List<PlanResponse>>();
        Assert.NotNull(plans);
        Assert.Contains(plans!, p => p.Code == "free");
        Assert.Contains(plans!, p => p.Code == "pro");
    }

    [Fact]
    public async Task Change_plan_to_pro_updates_subscription()
    {
        var client = await RegisterAndLoginAsync("subco3");

        var change = await client.PostAsJsonAsync("/api/v1/subscription/change-plan", new ChangePlanRequest("pro"));
        Assert.Equal(HttpStatusCode.OK, change.StatusCode);
        var updated = await change.Content.ReadFromJsonAsync<SubscriptionResponse>();
        Assert.NotNull(updated);
        Assert.Equal("pro", updated!.PlanCode);

        var res = await client.GetAsync("/api/v1/subscription");
        var sub = await res.Content.ReadFromJsonAsync<SubscriptionResponse>();
        Assert.Equal("pro", sub!.PlanCode);
    }

    [Fact]
    public async Task Change_plan_with_unknown_code_returns_400()
    {
        var client = await RegisterAndLoginAsync("subco4");

        var change = await client.PostAsJsonAsync("/api/v1/subscription/change-plan", new ChangePlanRequest("nonexistent"));
        Assert.Equal(HttpStatusCode.BadRequest, change.StatusCode);
    }
}
