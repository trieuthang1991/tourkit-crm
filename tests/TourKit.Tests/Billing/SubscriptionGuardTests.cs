using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TourKit.Api.Auth;
using TourKit.Api.Tenancy;
using TourKit.Shared.Entities;
using TourKit.Infrastructure.Persistence;
using TourKit.Tests.Support;

using TourKit.Shared.Enums;

namespace TourKit.Tests.Billing;

public class SubscriptionGuardTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;

    public SubscriptionGuardTests(AuthTestFactory factory) => _factory = factory;

    private async Task<HttpClient> LoginAsync(string slug)
    {
        var (s, email, pw) = await _factory.SeedTenantUserAsync(slug);
        var client = _factory.CreateClient();
        var auth = await (await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(s, email, pw)))
            .Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        return client;
    }

    [Fact]
    public async Task No_subscription_is_allowed()   // ân hạn: chưa có subscription → không chặn
    {
        var client = await LoginAsync("guard-nosub");
        var res = await client.GetAsync("/api/v1/customers");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Expired_subscription_blocks_business_endpoints_with_402()
    {
        var client = await LoginAsync("guard-expired");

        // gắn subscription HẾT HẠN cho tenant này (qua DbContext)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var tenantCtx = scope.ServiceProvider.GetRequiredService<AmbientTenantContext>();
            var tenant = await db.Tenants.FirstAsync(t => t.Slug == "guard-expired");
            tenantCtx.SetTenant(tenant.Id);
            var plan = await db.Plans.FirstAsync();
            db.Subscriptions.Add(new Subscription
            {
                PlanId = plan.Id, Status = SubscriptionStatus.Expired, StartedAt = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync();
        }

        // endpoint nghiệp vụ → 402
        var biz = await client.GetAsync("/api/v1/customers");
        Assert.Equal(HttpStatusCode.PaymentRequired, biz.StatusCode);

        // billing được miễn trừ (còn xem/gia hạn được)
        var billing = await client.GetAsync("/api/v1/subscription");
        Assert.NotEqual(HttpStatusCode.PaymentRequired, billing.StatusCode);
    }
}
