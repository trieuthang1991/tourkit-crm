using System.Net;
using TourKit.Tests.Support;

namespace TourKit.Tests.Web;

public class RazorPagesSmokeTests : IClassFixture<AuthTestFactory>
{
    private readonly AuthTestFactory _factory;
    public RazorPagesSmokeTests(AuthTestFactory factory) => _factory = factory;

    [Fact]
    public async Task Ping_page_renders_anonymously()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/Ping");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var html = await res.Content.ReadAsStringAsync();
        Assert.Contains("pong", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Customers_page_redirects_to_login_when_anonymous()
    {
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        var res = await client.GetAsync("/Customers");
        Assert.Equal(HttpStatusCode.Found, res.StatusCode); // 302 → /Auth/Login
        Assert.Contains("/Auth/Login", res.Headers.Location?.OriginalString ?? "");
    }

    [Fact]
    public async Task Login_page_renders_anonymously()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/Auth/Login");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var html = await res.Content.ReadAsStringAsync();
        Assert.Contains("Đăng nhập", html);
    }
}
