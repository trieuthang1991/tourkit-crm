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
    public async Task Legacy_url_301_redirects_to_vietnamese_route()
    {
        // URL cũ PascalCase (/Customers) phải 301 sang route tiếng Việt (/khach-hang) —
        // LegacyRouteRedirectMiddleware. Giữ bookmark/link cũ không chết.
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        var res = await client.GetAsync("/Customers");
        Assert.Equal(HttpStatusCode.MovedPermanently, res.StatusCode); // 301
        Assert.Contains("/khach-hang", res.Headers.Location?.OriginalString ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Protected_page_redirects_to_login_when_anonymous()
    {
        // Trang cần đăng nhập, truy cập ẩn danh → 302 sang trang đăng nhập tiếng Việt.
        var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        var res = await client.GetAsync("/khach-hang");
        Assert.Equal(HttpStatusCode.Found, res.StatusCode); // 302 → /dang-nhap
        Assert.Contains("/dang-nhap", res.Headers.Location?.OriginalString ?? "", StringComparison.Ordinal);
    }

    [Fact]
    public async Task Login_page_renders_anonymously()
    {
        var client = _factory.CreateClient();
        var res = await client.GetAsync("/dang-nhap");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var html = await res.Content.ReadAsStringAsync();
        Assert.Contains("Đăng nhập", html, StringComparison.Ordinal);
    }
}
