using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TourKit.Infrastructure.Persistence;

namespace TourKit.Tests.Api;

public class CustomerEndpointIsolationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CustomerEndpointIsolationTests(WebApplicationFactory<Program> factory)
    {
        // Thay SQL Server bằng InMemory để test không cần DB thật.
        // EF Core 10 đăng ký cả DbContextOptions<> lẫn IDbContextOptionsConfiguration<> (cấu hình provider);
        // phải gỡ CẢ HAI, nếu không SqlServer + InMemory cùng active → "single provider" lỗi.
        _factory = factory.WithWebHostBuilder(b => b.ConfigureServices(services =>
        {
            var toRemove = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>) ||
                (d.ServiceType.IsGenericType &&
                 d.ServiceType.GetGenericTypeDefinition().Name == "IDbContextOptionsConfiguration`1" &&
                 d.ServiceType.GenericTypeArguments[0] == typeof(AppDbContext))).ToList();
            foreach (var d in toRemove)
            {
                services.Remove(d);
            }

            services.AddDbContext<AppDbContext>(opt =>
                opt.UseInMemoryDatabase("EndpointIsolation"));
        }));
    }

    [Fact(Skip = "Chuyển sang JWT ở Task 7 (0b-1)")]
    public async Task Each_tenant_sees_only_its_own_customers_over_http()
    {
        var tenantA = Guid.NewGuid().ToString();
        var tenantB = Guid.NewGuid().ToString();
        var client = _factory.CreateClient();

        // tenant A tạo khách "A-http"
        var reqA = new HttpRequestMessage(HttpMethod.Post, "/api/v1/customers");
        reqA.Headers.Add("X-Tenant-Id", tenantA);
        reqA.Content = JsonContent.Create(new { FullName = "A-http", Phone = (string?)null });
        (await client.SendAsync(reqA)).EnsureSuccessStatusCode();

        // tenant B đọc — không được thấy khách của A
        var reqB = new HttpRequestMessage(HttpMethod.Get, "/api/v1/customers");
        reqB.Headers.Add("X-Tenant-Id", tenantB);
        var resB = await client.SendAsync(reqB);
        var listB = await resB.Content.ReadFromJsonAsync<List<CustomerDto>>();

        Assert.NotNull(listB);
        Assert.Empty(listB!);
    }

    private sealed record CustomerDto(Guid Id, string FullName, string? Phone);
}
